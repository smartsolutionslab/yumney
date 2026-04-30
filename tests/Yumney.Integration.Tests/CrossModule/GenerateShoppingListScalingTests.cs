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
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// Locks in the scaling and merging math in
/// <c>GenerateShoppingListCommandHandler</c>. Two recipes share an
/// ingredient (Pasta, in grams) but are planned with serving counts that do
/// not equal their recipe-default servings — so each recipe contributes a
/// different *scaled* quantity, and the merge step must sum them. A bug in
/// the scale factor, the merge key, or the rounding strategy would all be
/// caught here while the existing flow tests assert only "items > 0".
/// </summary>
[Collection(AspireCollection.Name)]
public class GenerateShoppingListScalingTests(AspireFixture fixture) : IAsyncLifetime
{
	private const int TestWeek = 43;

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	private static string WeekPath => $"/api/v1/meal-plans/{Year}/w/{TestWeek}";

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task GenerateShoppingList_TwoRecipesShareIngredient_MergesScaledQuantities()
	{
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");
		using var shoppingClient = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		// Recipe A: Pasta 200g, default 4 servings — planned for 6 servings → scale 1.5 → 300g.
		var recipeATitle = $"ScaleA-{Guid.NewGuid():N}";
		var recipeAId = await SaveRecipeAsync(
			recipesClient,
			recipeATitle,
			recipeServings: 4,
			ingredients: [("Pasta", 200m, "g")]);

		// Recipe B: Pasta 100g + Tomato Sauce 200ml, default 2 servings —
		// planned for 3 servings → scale 1.5 → 150g pasta + 300ml sauce.
		// Tomato Sauce avoids the default staples list (salt/pepper/olive oil/
		// vegetable oil/flour/sugar/butter/eggs/garlic/onion) — those are
		// stripped out of the generated shopping list and would mask the
		// merge assertion below.
		var recipeBTitle = $"ScaleB-{Guid.NewGuid():N}";
		var recipeBId = await SaveRecipeAsync(
			recipesClient,
			recipeBTitle,
			recipeServings: 2,
			ingredients: [("Pasta", 100m, "g"), ("Tomato Sauce", 200m, "ml")]);

		// Assign both to the same week with the chosen serving counts.
		await AssignRecipeAsync(mealplanClient, recipeAId, recipeATitle, DayOfWeek.Monday, plannedServings: 6);
		await AssignRecipeAsync(mealplanClient, recipeBId, recipeBTitle, DayOfWeek.Tuesday, plannedServings: 3);

		// Wait for both planned recipes to land in the read model.
		await Eventually.AssertAsync(
			async () =>
			{
				var response = await mealplanClient.GetAsync($"{WeekPath}/planned-recipes");
				var planned = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
				var titles = planned.GetProperty("recipes").EnumerateArray()
					.Select(r => r.GetProperty("recipeTitle").GetString())
					.ToList();
				titles.Should().Contain(recipeATitle);
				titles.Should().Contain(recipeBTitle);
			},
			timeout: TimeSpan.FromSeconds(15));

		// Generate. The handler scales each recipe's ingredients to the planned
		// servings, then merges by case-insensitive name + unit key.
		var generateResponse = await mealplanClient.PostAsync($"{WeekPath}/generate-shopping-list", null);
		generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
		var generated = await generateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

		// Two distinct items hit the shopping list: merged Pasta and Garlic.
		// (If the merge key broke, this would be 3.)
		generated.GetProperty("itemsAdded").GetInt32().Should().Be(2);

		// Verify the merged read model carries the *summed* scaled totals,
		// not the unscaled ones from either recipe alone.
		await Eventually.AssertAsync(
			async () =>
			{
				var response = await shoppingClient.GetAsync("/api/v1/shopping-lists/merged");
				response.StatusCode.Should().Be(HttpStatusCode.OK);
				var merged = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
				var items = merged.GetProperty("items").EnumerateArray().ToList();

				var pasta = items.SingleOrDefault(i =>
					string.Equals(i.GetProperty("itemName").GetString(), "Pasta", StringComparison.OrdinalIgnoreCase) &&
					i.GetProperty("unit").GetString() == "g");
				pasta.ValueKind.Should().NotBe(JsonValueKind.Undefined, "Pasta should be present once after the merge");
				pasta.GetProperty("totalQuantity").GetDecimal().Should().Be(450m, "200g × 1.5 + 100g × 1.5 = 450g");

				var sauce = items.SingleOrDefault(i =>
					string.Equals(i.GetProperty("itemName").GetString(), "Tomato Sauce", StringComparison.OrdinalIgnoreCase));
				sauce.ValueKind.Should().NotBe(JsonValueKind.Undefined);
				sauce.GetProperty("totalQuantity").GetDecimal().Should().Be(300m, "200ml × 1.5 = 300ml");
			},
			timeout: TimeSpan.FromSeconds(15));
	}

	private static async Task<Guid> SaveRecipeAsync(
		HttpClient client,
		string title,
		int recipeServings,
		(string Name, decimal Amount, string Unit)[] ingredients)
	{
		var request = new
		{
			title,
			ingredients = ingredients.Select(i => new { name = i.Name, amount = i.Amount, unit = i.Unit }).ToArray(),
			steps = new object[] { new { number = 1, description = "Cook." } },
			servings = recipeServings,
		};

		var response = await client.PostAsJsonAsync("/api/v1/recipes", request);
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task AssignRecipeAsync(
		HttpClient client,
		Guid recipeId,
		string title,
		DayOfWeek day,
		int plannedServings)
	{
		var request = new
		{
			day,
			recipeIdentifier = recipeId,
			recipeTitle = title,
			mealType = 0,
			servings = plannedServings,
		};

		var response = await client.PostAsJsonAsync($"{WeekPath}/slots", request);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
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
