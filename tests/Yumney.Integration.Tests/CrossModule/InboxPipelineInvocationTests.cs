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
using SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// Locks in that Shopping's <c>EfCoreInboxStore</c> is actually consulted by
/// the Wolverine consumer pipeline — not just registered in DI. The generic
/// <c>IntegrationEventConsumer&lt;T&gt;</c> calls
/// <c>IInboxStore.TryMarkProcessedAsync</c> *before* invoking each handler;
/// a successful call leaves a row keyed by (messageId, fully-qualified
/// handler type name) in the <c>InboxMessages</c> table.
///
/// Without this test, a misconfigured DI graph (e.g. `TryAddScoped` losing
/// to a later `NoOpInboxStore` registration, or the consumer skipping the
/// inbox call entirely) would not be detectable from outside — handlers
/// would still run, but redelivery dedup would be silently broken.
/// </summary>
[Collection(AspireCollection.Name)]
public class InboxPipelineInvocationTests(AspireFixture fixture) : IAsyncLifetime
{
	private const int TestWeek = 41;
	private const DayOfWeek TestDay = DayOfWeek.Wednesday;
	private const int TestMealType = 0; // Dinner

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	private static string WeekPath => $"/api/v1/meal-plans/{Year}/w/{TestWeek}";

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task ConfirmMealAsCooked_FlowsThroughInboxStore_BeforeHandlerInvocation()
	{
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var recipeTitle = $"InboxTest-{Guid.NewGuid():N}";
		var recipeId = await SaveRecipeAsync(recipesClient, recipeTitle);
		await AssignRecipeAsync(mealplanClient, recipeId, recipeTitle);
		await WaitForPlannedRecipeAsync(mealplanClient, recipeTitle);

		// Capture the timestamp boundary before triggering the cross-module event,
		// so the assertion can ignore inbox rows produced by earlier tests in
		// the same Aspire collection.
		var startedAt = DateTime.UtcNow.AddSeconds(-1); // 1s slack for clock drift

		await ConfirmMealCookedAsync(mealplanClient);

		// Wolverine routes MealConfirmedIntegrationEvent → IntegrationEventConsumer<T>
		// → IInboxStore.TryMarkProcessedAsync(messageId, "...MealConfirmedHandler")
		// → MealConfirmedHandler. Poll the inbox table until the dedup row lands.
		var expectedConsumer = typeof(MealConfirmedHandler).FullName!;

		await Eventually.AssertAsync(
			async () =>
			{
				await using var ctx = await fixture.CreateShoppingDbContextAsync();
				var matchingRow = await ctx.InboxMessages
					.Where(m => m.ConsumerName == expectedConsumer && m.ProcessedAt >= startedAt)
					.OrderByDescending(m => m.ProcessedAt)
					.FirstOrDefaultAsync();

				matchingRow.Should().NotBeNull(
					$"the consumer pipeline must record an inbox row for {expectedConsumer} after the event is dispatched");
				matchingRow!.MessageId.Should().NotBe(Guid.Empty);
			},
			timeout: TimeSpan.FromSeconds(20));
	}

	private static async Task<Guid> SaveRecipeAsync(HttpClient client, string title)
	{
		var request = new
		{
			title,
			ingredients = new object[] { new { name = "Salt", amount = 1m, unit = "g" } },
			steps = new object[] { new { number = 1, description = "Season." } },
			servings = 2,
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
			servings = 2,
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

		await fixture.ResetShoppingListEventStoreAsync(owner);
		await fixture.ResetShoppingEventStoreAsync(owner);

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

		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(r =>
				r.Owner == global::SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.OwnerIdentifier.From(userId)));
	}
}
