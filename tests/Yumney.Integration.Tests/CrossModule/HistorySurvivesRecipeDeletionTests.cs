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
/// MealPlan history (US-331) is built from denormalized slot read items —
/// the recipe title is captured at assignment time, so deleting the source
/// recipe afterwards must not erase the cooked-meal record.
///
/// MealPlan does not subscribe to <c>RecipeDeletedIntegrationEvent</c>; this
/// test pins that contract so a future "convenience" subscriber that
/// nulls out RecipeTitle on slot rows would fail the assertion instead of
/// silently shredding history.
/// </summary>
[Collection(AspireCollection.Name)]
public class HistorySurvivesRecipeDeletionTests(AspireFixture fixture) : IAsyncLifetime
{
	private const int TestWeek = 42;
	private const DayOfWeek TestDay = DayOfWeek.Thursday;
	private const int TestMealType = 0; // Dinner

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	private static string WeekPath => $"/api/v1/meal-plans/{Year}/w/{TestWeek}";

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task DeleteRecipe_AfterMealCooked_LeavesHistoryEntryIntact()
	{
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var recipeTitle = $"HistoryTest-{Guid.NewGuid():N}";

		// 1. Save → assign → cook the meal so a history row is materialized.
		var recipeId = await SaveRecipeAsync(recipesClient, recipeTitle);
		await AssignRecipeAsync(mealplanClient, recipeId, recipeTitle);
		await WaitForPlannedRecipeAsync(mealplanClient, recipeTitle);
		await ConfirmMealCookedAsync(mealplanClient);

		// 2. Wait until the cooked entry is searchable in history (projection lag).
		await Eventually.AssertAsync(
			async () =>
			{
				var titles = await SearchHistoryTitlesAsync(mealplanClient, recipeTitle);
				titles.Should().Contain(recipeTitle);
			},
			timeout: TimeSpan.FromSeconds(15));

		// 3. Delete the source recipe. RecipeDeletedIntegrationEvent fires on
		//    the bus and Shopping reacts to it — MealPlan does not subscribe.
		var deleteResponse = await recipesClient.DeleteAsync($"/api/v1/recipes/{recipeId}");
		deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		// 4. The history entry must survive — give the bus enough time to
		//    deliver the deletion event everywhere, then re-assert. Eventually
		//    polls with retries so a flaky 'still settling' read can't
		//    false-positive: the entry must remain present for the full window.
		await Task.Delay(TimeSpan.FromSeconds(2));

		var afterDelete = await SearchHistoryTitlesAsync(mealplanClient, recipeTitle);
		afterDelete.Should().Contain(
			recipeTitle,
			"deleting the source recipe must not cascade into the meal-plan history projection");
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
					.Select(entry => entry.GetProperty("recipeTitle").GetString())
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

	private static async Task<System.Collections.Generic.List<string?>> SearchHistoryTitlesAsync(
		HttpClient mealplanClient,
		string term)
	{
		var response = await mealplanClient.GetAsync(
			$"/api/v1/meal-plans/history/search?term={Uri.EscapeDataString(term)}");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("items").EnumerateArray()
			.Select(entry => entry.GetProperty("recipeTitle").GetString())
			.ToList();
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
