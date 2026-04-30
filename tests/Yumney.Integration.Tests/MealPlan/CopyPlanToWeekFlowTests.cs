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

/// <summary>
/// Flow tests for POST
/// /api/v1/meal-plans/{srcYear}/w/{srcWeek}/copy-to/{dstYear}/{dstWeek}.
/// Drives the full path: an authenticated owner assigns a recipe to a source
/// week, copies it to a destination week, and reads the destination back to
/// confirm the slot survived the copy through the event store + projection.
/// Plus the two declared error paths (SourcePlanNotFound, SameWeek) at the
/// HTTP boundary.
/// </summary>
[Collection(AspireCollection.Name)]
public class CopyPlanToWeekFlowTests(AspireFixture fixture)
{
	private const int SourceWeek = 39;
	private const int TargetWeek = 38;

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static int Year => DateTime.UtcNow.Year;

	[Fact]
	public async Task CopyPlanToWeek_SourceHasRecipeAssignment_TargetWeekShowsSameSlot()
	{
		using var recipesClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var recipeTitle = $"CopyTest-{Guid.NewGuid():N}";
		var recipeId = await SaveRecipeAsync(recipesClient, recipeTitle);

		// Assign on Monday Dinner of the source week.
		var assignRequest = new
		{
			day = DayOfWeek.Monday,
			recipeIdentifier = recipeId,
			recipeTitle,
			mealType = 0,
			servings = 4,
		};
		var assign = await mealplanClient.PostAsJsonAsync(
			$"/api/v1/meal-plans/{Year}/w/{SourceWeek}/slots", assignRequest);
		assign.StatusCode.Should().Be(HttpStatusCode.OK);

		// Wait for the source week's projection so the read-back assertion below
		// can also rely on the projection pipeline working end-to-end.
		await Eventually.AssertAsync(
			async () =>
			{
				var sourcePlan = await GetWeekPlanAsync(mealplanClient, SourceWeek);
				FindMondayDinner(sourcePlan).GetProperty("recipeTitle").GetString().Should().Be(recipeTitle);
			},
			timeout: TimeSpan.FromSeconds(15));

		// Copy. The handler reads the source from the event store directly, not
		// from the read model — so this call doesn't depend on projection lag.
		var copy = await mealplanClient.PostAsync(
			$"/api/v1/meal-plans/{Year}/w/{SourceWeek}/copy-to/{Year}/{TargetWeek}",
			content: null);
		copy.StatusCode.Should().Be(HttpStatusCode.OK);

		// The synchronous response already carries the target week's slots —
		// verify there before polling the read model.
		var copyBody = await copy.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var copiedMondayDinner = FindMondayDinner(copyBody);
		copiedMondayDinner.GetProperty("recipeTitle").GetString().Should().Be(recipeTitle);
		copiedMondayDinner.GetProperty("servings").GetInt32().Should().Be(4);

		// Read-model projection of the *target* week catches up async — confirm
		// that GET shows the same slot as the synchronous response.
		await Eventually.AssertAsync(
			async () =>
			{
				var targetPlan = await GetWeekPlanAsync(mealplanClient, TargetWeek);
				var slot = FindMondayDinner(targetPlan);
				slot.GetProperty("recipeTitle").GetString().Should().Be(recipeTitle);
			},
			timeout: TimeSpan.FromSeconds(15));
	}

	[Fact]
	public async Task CopyPlanToWeek_SourcePlanDoesNotExist_Returns404()
	{
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		// A week the owner has never touched — handler returns SourcePlanNotFound.
		var response = await mealplanClient.PostAsync(
			$"/api/v1/meal-plans/{Year}/w/2/copy-to/{Year}/3",
			content: null);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CopyPlanToWeek_SourceEqualsTarget_Returns422()
	{
		using var mealplanClient = await fixture.CreateAuthenticatedClientAsync("mealplan-api");

		var response = await mealplanClient.PostAsync(
			$"/api/v1/meal-plans/{Year}/w/{SourceWeek}/copy-to/{Year}/{SourceWeek}",
			content: null);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task CopyPlanToWeek_Unauthenticated_Returns401()
	{
		var client = fixture.MealPlanApi;

		var response = await client.PostAsync(
			$"/api/v1/meal-plans/{Year}/w/{SourceWeek}/copy-to/{Year}/{TargetWeek}",
			content: null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private static async Task<Guid> SaveRecipeAsync(HttpClient client, string title)
	{
		var request = new
		{
			title,
			ingredients = new object[] { new { name = "Salt", amount = 1m, unit = "g" } },
			steps = new object[] { new { number = 1, description = "Season." } },
			servings = 4,
		};

		var response = await client.PostAsJsonAsync("/api/v1/recipes", request);
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task<JsonElement> GetWeekPlanAsync(HttpClient mealplanClient, int week)
	{
		var response = await mealplanClient.GetAsync($"/api/v1/meal-plans/{Year}/w/{week}");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
	}

	private static JsonElement FindMondayDinner(JsonElement plan)
	{
		var slot = plan.GetProperty("slots").EnumerateArray().FirstOrDefault(s =>
			s.GetProperty("day").GetString() == "Monday" &&
			s.GetProperty("mealType").GetString() == "Dinner" &&
			!s.GetProperty("isEmpty").GetBoolean());
		slot.ValueKind.Should().NotBe(
			JsonValueKind.Undefined,
			"Monday Dinner slot must be populated for the assertion to be meaningful");
		return slot;
	}
}
