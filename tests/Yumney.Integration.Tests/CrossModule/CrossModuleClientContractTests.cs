using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Shopping.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.CrossModule;

/// <summary>
/// Phase 2b/2c/2d (#644-#646) coverage: validates that the URLs Yumney.MealPlan.Client
/// and Yumney.Shopping.Client emit actually exist on the live module APIs and that
/// the JSON shapes match the response records the clients deserialize against.
///
/// The unit tests for the chat tools and HTTP adapters all use stubbed
/// IMealPlanClient / IShoppingClient — so a wrong URL ("/api/v1/meal-plan"
/// instead of "/api/v1/meal-plans") would slip through. These tests exercise
/// the actual endpoint contract.
/// </summary>
[Collection(AspireCollection.Name)]
public class CrossModuleClientContractTests(AspireFixture fixture)
{
	[Fact]
	public async Task MealPlanWeeklyEndpoint_AtClientUrl_ReturnsParseableWeeklyPlanResponse()
	{
		var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.GetAsync("/api/v1/meal-plans/2026/w/19");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var parsed = await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>();
		parsed.Should().NotBeNull();
		parsed!.Slots.Should().NotBeNull();
	}

	[Fact]
	public async Task MealPlanAssignSlotsEndpoint_AcceptsAssignRecipeBody()
	{
		var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");
		var body = new AssignRecipeBody(
			Day: "Monday",
			RecipeIdentifier: Guid.NewGuid(),
			RecipeTitle: "Carbonara",
			MealType: "Dinner",
			Servings: 4);

		var response = await client.PostAsJsonAsync("/api/v1/meal-plans/2026/w/19/slots", body);

		// We don't require 2xx — the recipe doesn't exist, MealPlan may reject
		// with 400/404. The point is: the endpoint binds AssignRecipeBody (the
		// shape my MealPlanClient sends) — anything other than 415 / 405 / 404
		// route-not-found proves the URL + body shape match.
		response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
		response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
		response.StatusCode.Should().NotBe(HttpStatusCode.UnsupportedMediaType);
	}

	[Fact]
	public async Task MealPlanConfirmEndpoint_AcceptsConfirmMealBody()
	{
		var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");
		var body = new ConfirmMealBody(Day: "Monday", MealType: "Dinner", State: "Cooked");

		var response = await client.PutAsJsonAsync("/api/v1/meal-plans/2026/w/19/slots/confirm", body);

		response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
		response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
		response.StatusCode.Should().NotBe(HttpStatusCode.UnsupportedMediaType);
	}

	[Fact]
	public async Task ShoppingMergedEndpoint_AtClientUrl_ReturnsParseableMergedShoppingListResponse()
	{
		var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.GetAsync("/api/v1/shopping-lists/merged?includePastBought=false");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var parsed = await response.Content.ReadFromJsonAsync<MergedShoppingListResponse>();
		parsed.Should().NotBeNull();
		parsed!.Items.Should().NotBeNull();
	}

	[Fact]
	public async Task ShoppingFromRecipesEndpoint_AcceptsCreateListFromRecipesBody()
	{
		var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var body = new CreateListFromRecipesBody(
			Title: "Integration test list",
			Recipes: [new CreateListRecipeBody(Guid.NewGuid(), Servings: null)]);

		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists/from-recipes", body);

		// Same logic as MealPlan write tests — UnprocessableEntity / BadRequest
		// is fine (recipe doesn't exist), but route-not-found / wrong-shape
		// failures would prove the contract is broken.
		response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
		response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
		response.StatusCode.Should().NotBe(HttpStatusCode.UnsupportedMediaType);
	}

	[Fact]
	public async Task MealPlanWeeklyEndpoint_RouteShapeMatchesClientPattern()
	{
		// Belt-and-braces: the MealPlanClient builds the URL via
		// $"/api/v1/meal-plans/{year}/w/{weekNumber}". Any drift in the route
		// template (e.g. someone renames {year} → {y}) would break this.
		var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var goodResponse = await client.GetAsync("/api/v1/meal-plans/2026/w/19");
		goodResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var badResponse = await client.GetAsync("/api/v1/meal-plans/2026/19");
		badResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}
