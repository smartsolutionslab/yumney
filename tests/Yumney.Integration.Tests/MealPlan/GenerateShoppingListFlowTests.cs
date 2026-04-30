using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.MealPlan;

[Collection(AspireCollection.Name)]
public class GenerateShoppingListFlowTests(AspireFixture fixture)
{
	private const int TestWeek = 52;

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	private static string WeekPath => $"/api/v1/meal-plans/{Year}/w/{TestWeek}";

	[Fact(Skip = "Requires auth propagation in HTTP adapters (service-to-service calls don't forward JWT)")]
	public async Task GenerateShoppingList_WithRecipes_ReturnsItemCount()
	{
		// 1. Create a recipe with ingredients via recipes-api
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var recipeRequest = new
		{
			title = "Shopping Gen Recipe",
			ingredients = new object[]
			{
				new { name = "Flour", amount = 500m, unit = "g" },
				new { name = "Sugar", amount = 200m, unit = "g" },
				new { name = "Butter", amount = 100m, unit = "g" },
			},
			steps = new object[] { new { number = 1, description = "Mix all" } },
			servings = 4,
		};

		var recipeResponse = await recipesClient.PostAsJsonAsync("/api/v1/recipes", recipeRequest);
		recipeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var recipe = await recipeResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var recipeId = recipe.GetProperty("identifier").GetGuid();

		// 2. Assign recipe to meal plan via mealplan-api
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var assignRequest = new
		{
			day = DayOfWeek.Monday,
			recipeIdentifier = recipeId,
			recipeTitle = "Shopping Gen Recipe",
			mealType = 0,
			servings = 4,
		};

		var assignResponse = await mealplanClient.PostAsJsonAsync($"{WeekPath}/slots", assignRequest);
		assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		// 3. Generate shopping list
		var generateResponse = await mealplanClient.PostAsync($"{WeekPath}/generate-shopping-list", null);

		generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await generateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		result.GetProperty("itemsAdded").GetInt32().Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task GenerateShoppingList_EmptyPlan_ReturnsError()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		// Use a week with no assignments
		var response = await client.PostAsync($"/api/v1/meal-plans/{Year}/w/1/generate-shopping-list", null);

		// Should fail — no recipes assigned
		response.StatusCode.Should().BeOneOf(
			HttpStatusCode.BadRequest,
			HttpStatusCode.NotFound,
			HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task GenerateShoppingList_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.PostAsync($"{WeekPath}/generate-shopping-list", null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
