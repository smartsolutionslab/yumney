using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.MealPlan.Contract;

/// <summary>
/// Contract tests for MealPlan mutation endpoints not covered by the existing
/// flow tests: ToggleExtendedMode, AdjustServings, SwapSlots, ConfirmMeal,
/// CookWithLeftovers, GetPlannedRecipes.
///
/// Tests target a far-future year so they do not collide with real-year data.
/// </summary>
[Collection(AspireCollection.Name)]
public class MealPlanMutationContractTests(AspireFixture fixture)
{
	private const int Year = 2099;

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task ToggleExtendedMode_Enable_Returns200()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.PutAsJsonAsync(WeekPath(10) + "/extended-mode", new { enable = true });

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task ToggleExtendedMode_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.PutAsJsonAsync(WeekPath(10) + "/extended-mode", new { enable = true });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task AdjustServings_SlotExists_Returns200WithUpdatedPlan()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");
		var week = WeekPath(11);
		await client.PostAsJsonAsync(week + "/slots", new
		{
			day = DayOfWeek.Monday,
			recipeIdentifier = Guid.NewGuid(),
			recipeTitle = "Test",
			mealType = 0,
			servings = 4,
		});

		var response = await client.PutAsJsonAsync(week + "/slots/servings", new
		{
			day = DayOfWeek.Monday,
			mealType = 0,
			servings = 6,
		});

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task AdjustServings_ZeroServings_Returns400GuardFailure()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.PutAsJsonAsync(WeekPath(12) + "/slots/servings", new
		{
			day = DayOfWeek.Monday,
			mealType = 0,
			servings = 0,
		});

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task AdjustServings_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.PutAsJsonAsync(WeekPath(12) + "/slots/servings", new
		{
			day = DayOfWeek.Monday,
			mealType = 0,
			servings = 4,
		});

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task SwapSlots_SlotsNotAssigned_Returns404()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.PutAsJsonAsync(WeekPath(13) + "/slots/swap", new
		{
			sourceDay = DayOfWeek.Friday,
			targetDay = DayOfWeek.Saturday,
			mealType = 0,
		});

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task SwapSlots_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.PutAsJsonAsync(WeekPath(13) + "/slots/swap", new
		{
			sourceDay = DayOfWeek.Monday,
			targetDay = DayOfWeek.Tuesday,
			mealType = 0,
		});

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ConfirmMeal_SlotExists_Returns200()
	{
		// Uses state=2 (Skipped) instead of 1 (Cooked) — Cooked triggers the
		// cross-module ingredient lookup added in #280, which needs a real
		// recipe. End-to-end Cooked propagation belongs in a dedicated
		// cross-module test, not this contract test.
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");
		var week = WeekPath(14);
		await client.PostAsJsonAsync(week + "/slots", new
		{
			day = DayOfWeek.Wednesday,
			recipeIdentifier = Guid.NewGuid(),
			recipeTitle = "Wed Meal",
			mealType = 0,
			servings = 4,
		});

		var response = await client.PutAsJsonAsync(week + "/slots/confirm", new
		{
			day = DayOfWeek.Wednesday,
			mealType = 0,
			state = 2,
		});

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task ConfirmMeal_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.PutAsJsonAsync(WeekPath(14) + "/slots/confirm", new
		{
			day = DayOfWeek.Wednesday,
			mealType = 0,
			state = 1,
		});

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task CookWithLeftovers_Valid_Returns200()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.PostAsJsonAsync(WeekPath(15) + "/cook-with-leftovers", new
		{
			cookDay = DayOfWeek.Monday,
			recipeIdentifier = Guid.NewGuid(),
			recipeTitle = "Double Batch",
			totalServings = 6,
			eatServings = 3,
			leftoverDay = DayOfWeek.Tuesday,
			mealType = 0,
		});

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task CookWithLeftovers_ZeroTotalServings_Returns400GuardFailure()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.PostAsJsonAsync(WeekPath(16) + "/cook-with-leftovers", new
		{
			cookDay = DayOfWeek.Monday,
			recipeIdentifier = Guid.NewGuid(),
			recipeTitle = "Invalid",
			totalServings = 0,
			eatServings = 0,
			leftoverDay = DayOfWeek.Tuesday,
			mealType = 0,
		});

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CookWithLeftovers_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.PostAsJsonAsync(WeekPath(15) + "/cook-with-leftovers", new
		{
			cookDay = DayOfWeek.Monday,
			recipeIdentifier = Guid.NewGuid(),
			recipeTitle = "NoAuth",
			totalServings = 4,
			eatServings = 2,
			leftoverDay = DayOfWeek.Tuesday,
			mealType = 0,
		});

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetPlannedRecipes_Authenticated_Returns200WithObject()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.GetAsync(WeekPath(17) + "/planned-recipes");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.ValueKind.Should().Be(JsonValueKind.Object);
	}

	[Fact]
	public async Task GetPlannedRecipes_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.GetAsync(WeekPath(17) + "/planned-recipes");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private static string WeekPath(int week) => $"/api/v1/meal-plans/{Year}/w/{week}";
}
