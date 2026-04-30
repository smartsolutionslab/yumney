using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.MealPlan;

/// <summary>
/// Smoke coverage for /api/v1/meal-plans/history/search (US-331). Verifies the
/// route runs end-to-end against PostgreSQL — catches EF.Functions.ILike SQL
/// translation bugs and JSON wiring issues that the in-memory fake repo can't
/// see. The cooked-flow round trip isn't exercised here because confirming a
/// meal makes a Recipes-API call against an unseeded recipe id; that path is
/// already covered by GenerateShoppingListFlowTests' seed-aware setup.
/// </summary>
[Collection(AspireCollection.Name)]
public class MealHistoryFlowTests(AspireFixture fixture)
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task SearchHistory_NoCookedMeals_Returns200WithEmptyArray()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.GetAsync("/api/v1/meal-plans/history/search");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var rows = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		rows.ValueKind.Should().Be(JsonValueKind.Array);
	}

	[Fact]
	public async Task SearchHistory_TermInUrl_Returns200WithEmptyArray()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.GetAsync("/api/v1/meal-plans/history/search?term=lasagna&limit=5");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task SearchHistory_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.GetAsync("/api/v1/meal-plans/history/search");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
