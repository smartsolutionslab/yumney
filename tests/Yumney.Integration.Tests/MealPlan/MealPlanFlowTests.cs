using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.MealPlan;

[Collection(AspireCollection.Name)]
public class MealPlanFlowTests(AspireFixture fixture)
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	private static int Week => System.Globalization.ISOWeek.GetWeekOfYear(DateTime.UtcNow);

	private static string WeekPath => $"/api/v1/meal-plans/{Year}/w/{Week}";

	[Fact]
	public async Task GetWeeklyPlan_EmptyWeek_ReturnsEmptySlots()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await client.GetAsync(WeekPath);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var plan = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		plan.GetProperty("week").GetString().Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task AssignRecipe_ValidSlot_ReturnsOk()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var recipeId = Guid.NewGuid();
		var request = new
		{
			day = DayOfWeek.Monday,
			recipeIdentifier = recipeId,
			recipeTitle = "Test Recipe",
			mealType = 0,
			servings = 4,
		};

		var response = await client.PostAsJsonAsync($"{WeekPath}/slots", request);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var plan = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var slots = plan.GetProperty("slots");
		slots.GetArrayLength().Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task AssignAndClearSlot_RemovesAssignment()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		// Assign
		var assignRequest = new
		{
			day = DayOfWeek.Tuesday,
			recipeIdentifier = Guid.NewGuid(),
			recipeTitle = "Temp Recipe",
			mealType = 0,
		};
		await client.PostAsJsonAsync($"{WeekPath}/slots", assignRequest);

		// Clear
		var clearRequest = new { day = DayOfWeek.Tuesday, mealType = 0 };
		var clearResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{WeekPath}/slots")
		{
			Content = JsonContent.Create(clearRequest),
		});

		clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task AssignRecipe_ThenGetPlan_SlotIsPopulated()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var recipeId = Guid.NewGuid();
		var request = new
		{
			day = DayOfWeek.Wednesday,
			recipeIdentifier = recipeId,
			recipeTitle = "Wednesday Dinner",
			mealType = 0,
			servings = 2,
		};

		await client.PostAsJsonAsync($"{WeekPath}/slots", request);

		// MealPlan read-model projection is driven async by Wolverine; poll.
		var deadline = DateTime.UtcNow.AddSeconds(15);
		JsonElement wednesdayDinner = default;
		while (DateTime.UtcNow < deadline)
		{
			var response = await client.GetAsync(WeekPath);
			var plan = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
			var slots = plan.GetProperty("slots");
			wednesdayDinner = slots.EnumerateArray()
				.FirstOrDefault(s =>
					s.GetProperty("day").GetString() == "Wednesday" &&
					s.GetProperty("mealType").GetString() == "Dinner" &&
					!s.GetProperty("isEmpty").GetBoolean());
			if (wednesdayDinner.ValueKind != JsonValueKind.Undefined) break;
			await Task.Delay(250);
		}

		wednesdayDinner.GetProperty("recipeTitle").GetString().Should().Be("Wednesday Dinner");
		wednesdayDinner.GetProperty("servings").GetInt32().Should().Be(2);
	}

	[Fact]
	public async Task GetWeeklyPlan_WithoutAuth_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.GetAsync(WeekPath);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
