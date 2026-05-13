using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// Single end-to-end happy-path journey that exercises every major piece of the
/// backend stack working together: Keycloak (real OIDC token), all four APIs
/// behind JWT auth, the four PostgreSQL databases (recipesdb, mealplandb,
/// shoppingdb, usersdb), RabbitMQ-driven projections, and cross-service HTTP
/// calls with JWT forwarding (mealplan → recipes/users/shopping via
/// AuthTokenDelegatingHandler).
///
/// The test deliberately writes through public HTTP endpoints only, then reads
/// back both via HTTP and directly from the database to confirm projections
/// caught up. A failure in any of these layers fails the test.
/// </summary>
[Collection(AspireCollection.Name)]
public class HappyPathJourneyTests(AspireFixture fixture) : IAsyncLifetime
{
	private const int JourneyWeek = 47;
	private const DayOfWeek JourneyDay = DayOfWeek.Saturday;
	private const int JourneyMealType = 0; // Dinner

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	private static string WeekPath => $"/api/v1/meal-plans/{Year}/w/{JourneyWeek}";

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task UserJourney_FromRecipeImportToCookedHistory_PassesThroughEntireBackend()
	{
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");
		using var shoppingClient = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var recipeTitle = $"Journey-{Guid.NewGuid():N}";

		// 1. Save recipe (recipes-api → recipesdb).
		var recipeIdentifier = await SaveRecipeAsync(recipesClient, recipeTitle);
		await AssertRecipeIsPersistedAsync(recipeIdentifier, recipeTitle);

		// 2. Assign recipe to a meal slot (mealplan-api → mealplandb event store).
		await AssignRecipeToSlotAsync(mealplanClient, recipeIdentifier, recipeTitle);

		// 3. Wait for the planned-recipes projection driven via Wolverine/RabbitMQ.
		await WaitForPlannedRecipeAsync(mealplanClient, recipeTitle);

		// 4. Generate shopping list — exercises mealplan → recipes/users/shopping
		// over HTTP with JWT forwarding, plus a write to the shopping event store.
		var generated = await GenerateShoppingListAsync(mealplanClient);
		generated.GetProperty("itemsAdded").GetInt32().Should().BeGreaterThan(0);

		// 5. Wait for the merged shopping list projection (RabbitMQ → shoppingdb read model).
		await AssertMergedListContainsIngredientsAsync(shoppingClient);

		// 6. Mark the meal as cooked — emits MealMarkedAsCookedModuleEvent and
		// re-fetches ingredients from recipes-api (still JWT-forwarded).
		await ConfirmMealAsCookedAsync(mealplanClient);

		// 7. The cooked meal is now searchable in history (mealplan read model).
		await AssertHistoryContainsRecipeAsync(mealplanClient, recipeTitle);
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
			steps = new object[] { new { number = 1, description = "Cook pasta, add sauce, top with parmesan." } },
			servings = 4,
		};

		var response = await client.PostAsJsonAsync("/api/v1/recipes", request);
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task AssignRecipeToSlotAsync(HttpClient client, Guid recipeIdentifier, string title)
	{
		var request = new
		{
			day = JourneyDay,
			recipeIdentifier,
			recipeTitle = title,
			mealType = JourneyMealType,
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
				var recipes = planned.GetProperty("recipes").EnumerateArray()
					.Select(entry => entry.GetProperty("recipeTitle").GetString())
					.ToList();
				recipes.Should().Contain(title);
			},
			timeout: TimeSpan.FromSeconds(15));
	}

	private static async Task<JsonElement> GenerateShoppingListAsync(HttpClient mealplanClient)
	{
		var response = await mealplanClient.PostAsync($"{WeekPath}/generate-shopping-list", null);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
	}

	private static async Task AssertMergedListContainsIngredientsAsync(HttpClient shoppingClient)
	{
		await Eventually.AssertAsync(
			async () =>
			{
				var response = await shoppingClient.GetAsync("/api/v1/shopping-lists/merged");
				response.StatusCode.Should().Be(HttpStatusCode.OK);
				var merged = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
				var names = merged.GetProperty("items").EnumerateArray()
					.Select(entry => entry.GetProperty("itemName").GetString())
					.ToList();
				names.Should().Contain("Pasta");
				names.Should().Contain("Tomato Sauce");
				names.Should().Contain("Parmesan");
			},
			timeout: TimeSpan.FromSeconds(15));
	}

	private static async Task ConfirmMealAsCookedAsync(HttpClient mealplanClient)
	{
		var request = new
		{
			day = JourneyDay,
			mealType = JourneyMealType,
			state = 1, // MealState.Cooked
		};

		var response = await mealplanClient.PutAsJsonAsync($"{WeekPath}/slots/confirm", request);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	private static async Task AssertHistoryContainsRecipeAsync(HttpClient mealplanClient, string recipeTitle)
	{
		await Eventually.AssertAsync(
			async () =>
			{
				var response = await mealplanClient.GetAsync(
					$"/api/v1/meal-plans/history/search?term={Uri.EscapeDataString(recipeTitle)}");
				response.StatusCode.Should().Be(HttpStatusCode.OK);
				var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
				var titles = body.GetProperty("items").EnumerateArray()
					.Select(entry => entry.GetProperty("recipeTitle").GetString())
					.ToList();
				titles.Should().Contain(recipeTitle);
			},
			timeout: TimeSpan.FromSeconds(15));
	}

	private async Task AssertRecipeIsPersistedAsync(Guid recipeIdentifier, string expectedTitle)
	{
		await using var ctx = await fixture.CreateRecipesDbContextAsync();
		var recipeId = global::SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.RecipeIdentifier.From(recipeIdentifier);
		var recipe = await ctx.Recipes.SingleAsync(r => r.Id == recipeId);
		recipe.Title.Value.Should().Be(expectedTitle);
	}

	private async Task CleanupAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);

		await fixture.ResetShoppingListEventStoreAsync(owner);
		await fixture.ResetShoppingEventStoreAsync(owner);
		await fixture.ResetShoppingReadModelAsync(userId);

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var summaries = await ctx.Set<ShoppingListSummaryReadItem>()
				.Where(summary => summary.OwnerId == userId).ToListAsync();
			var items = await ctx.Set<ShoppingListItemReadItem>()
				.Where(item => item.OwnerId == userId).ToListAsync();
			ctx.RemoveRange(summaries);
			ctx.RemoveRange(items);
			await ctx.SaveChangesAsync();
		}

		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(recipe =>
				recipe.Owner == global::SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.OwnerIdentifier.From(userId)));
	}
}
