using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// Locks in the cross-module side effect of confirming a meal as cooked:
/// MealPlan publishes a <c>MealConfirmedIntegrationEvent</c> over Wolverine,
/// Shopping's <c>MealConfirmedHandler</c> consumes it and decrements the
/// owner's ledger via <c>MarkConsumed</c>. This is fire-and-forget in the
/// happy-path test — a regression that drops the handler registration or
/// breaks the event payload would silently pass everywhere else.
///
/// The test exercises every layer end-to-end: HTTP confirm in mealplan-api,
/// real Wolverine/RabbitMQ delivery, MealConfirmedHandler in shopping-api,
/// and shopping event-store persistence.
/// </summary>
[Collection(AspireCollection.Name)]
public class MealConfirmedLedgerConsumptionTests(AspireFixture fixture) : IAsyncLifetime
{
	private const int TestWeek = 44;
	private const DayOfWeek TestDay = DayOfWeek.Sunday;
	private const int TestMealType = 0; // Dinner

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	private static string WeekPath => $"/api/v1/meal-plans/{Year}/w/{TestWeek}";

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task ConfirmMealAsCooked_WithExistingLedger_EmitsConsumedEventsForEachIngredient()
	{
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");
		using var shoppingClient = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var recipeTitle = $"LedgerTest-{Guid.NewGuid():N}";

		// 1. Save a recipe with quantified ingredients.
		var recipeId = await SaveRecipeAsync(recipesClient, recipeTitle);

		// 2. Assign it to a meal slot.
		await AssignRecipeAsync(mealplanClient, recipeId, recipeTitle);

		// 3. Wait for the planned-recipes projection so MealPlan's read model
		//    knows about the assignment before we confirm.
		await WaitForPlannedRecipeAsync(mealplanClient, recipeTitle);

		// 4. Pre-populate the ledger. MealConfirmedHandler short-circuits if
		//    no ledger exists for the owner — without this seed step a
		//    successful no-op would masquerade as a passing test.
		await AddManualItemAsync(shoppingClient, "Pasta", 400m, "g");
		await AddManualItemAsync(shoppingClient, "Tomato Sauce", 250m, "ml");
		await AddManualItemAsync(shoppingClient, "Parmesan", 50m, "g");

		// 5. Confirm the meal cooked. This is where the cross-module event fires.
		await ConfirmMealCookedAsync(mealplanClient);

		// 6. Wolverine delivers MealConfirmedIntegrationEvent → MealConfirmedHandler
		//    appends Consumed events to the ledger event stream. Poll until they
		//    land — there is no synchronous handle on this hop.
		var userId = await fixture.GetTestUserIdAsync();
		await Eventually.AssertAsync(
			async () =>
			{
				await using var ctx = await fixture.CreateShoppingDbContextAsync();
				var ledgerAggregateIds = await ctx.Set<AggregateMetadata>()
					.Where(m => m.OwnerId == userId)
					.Select(m => m.AggregateId)
					.ToListAsync();

				ledgerAggregateIds.Should().NotBeEmpty(
					"the AddManualItem calls in step 4 should have created a ledger aggregate");

				var consumedEvents = await ctx.Set<StoredEvent>()
					.Where(e => ledgerAggregateIds.Contains(e.AggregateId)
						&& e.EventType == nameof(ShoppingItemConsumed))
					.ToListAsync();

				consumedEvents.Should().HaveCountGreaterThanOrEqualTo(
					3,
					"each of the three recipe ingredients should produce one Consumed event");
			},
			timeout: TimeSpan.FromSeconds(20));

		// 7. The IngredientBalance read model should reflect the consumption
		//    across the same projection pipeline. ConsumedTotal is
		//    incremented by ShoppingItemConsumedIntegrationEvent.
		await Eventually.AssertAsync(
			async () =>
			{
				await using var ctx = await fixture.CreateShoppingReadDbContextAsync();
				var pasta = await ctx.IngredientBalanceReadItems
					.SingleOrDefaultAsync(r => r.OwnerId == userId && r.ItemName == "Pasta");
				pasta.Should().NotBeNull();
				pasta!.ConsumedTotal.Should().Be(400m);
			},
			timeout: TimeSpan.FromSeconds(20));
	}

	private static async Task<Guid> SaveRecipeAsync(HttpClient client, string title)
	{
		var request = new
		{
			title,
			ingredients = new object[]
			{
				new { name = "Pasta", amount = 400m, unit = "g" },
				new { name = "Tomato Sauce", amount = 250m, unit = "ml" },
				new { name = "Parmesan", amount = 50m, unit = "g" },
			},
			steps = new object[] { new { number = 1, description = "Cook pasta and combine." } },
			servings = 4,
		};

		var response = await client.PostAsJsonAsync("/api/v1/recipes", request);
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task AssignRecipeAsync(HttpClient client, Guid recipeId, string title)
	{
		var request = new
		{
			day = TestDay,
			recipeIdentifier = recipeId,
			recipeTitle = title,
			mealType = TestMealType,
			servings = 4,
		};

		var response = await client.PostAsJsonAsync($"{WeekPath}/slots", request);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	private static async Task WaitForPlannedRecipeAsync(HttpClient mealplanClient, string title)
	{
		await Eventually.AssertAsync(
			async () =>
			{
				var response = await mealplanClient.GetAsync($"{WeekPath}/planned-recipes");
				response.StatusCode.Should().Be(HttpStatusCode.OK);
				var planned = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
				var titles = planned.GetProperty("recipes").EnumerateArray()
					.Select(r => r.GetProperty("recipeTitle").GetString())
					.ToList();
				titles.Should().Contain(title);
			},
			timeout: TimeSpan.FromSeconds(15));
	}

	private static async Task AddManualItemAsync(HttpClient shoppingClient, string name, decimal quantity, string unit)
	{
		var response = await shoppingClient.PostAsJsonAsync(
			"/api/v1/shopping-lists/items",
			new { name, quantity, unit });
		response.IsSuccessStatusCode.Should().BeTrue(
			$"AddManualItem must succeed to seed the ledger; got {response.StatusCode}");
	}

	private static async Task ConfirmMealCookedAsync(HttpClient mealplanClient)
	{
		var request = new
		{
			day = TestDay,
			mealType = TestMealType,
			state = 1, // MealState.Cooked
		};

		var response = await mealplanClient.PutAsJsonAsync($"{WeekPath}/slots/confirm", request);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	private async Task CleanupAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);

		await fixture.ResetShoppingEventStoreAsync(owner);
		await fixture.ResetShoppingListEventStoreAsync(owner);

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var summaries = await ctx.Set<ShoppingListSummaryReadItem>()
				.Where(s => s.OwnerId == userId).ToListAsync();
			var items = await ctx.Set<ShoppingListItemReadItem>()
				.Where(i => i.OwnerId == userId).ToListAsync();
			ctx.RemoveRange(summaries);
			ctx.RemoveRange(items);
			await ctx.SaveChangesAsync();
		}

		await using (var readCtx = await fixture.CreateShoppingReadDbContextAsync())
		{
			var balanceRows = await readCtx.IngredientBalanceReadItems
				.Where(r => r.OwnerId == userId).ToListAsync();
			readCtx.RemoveRange(balanceRows);
			await readCtx.SaveChangesAsync();
		}

		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(r =>
				r.Owner == global::SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.OwnerIdentifier.From(userId)));
	}
}
