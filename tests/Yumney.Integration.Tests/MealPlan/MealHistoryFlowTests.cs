using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.MealPlan;

/// <summary>
/// End-to-end coverage for US-331 — exercises the actual EF query (ILike)
/// against PostgreSQL plus the full Wolverine projection pipeline. Unit tests
/// over the FakeMealPlanReadModelRepository don't catch SQL translation bugs.
/// </summary>
[Collection(AspireCollection.Name)]
public class MealHistoryFlowTests(AspireFixture fixture)
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task SearchHistory_AfterCookingARecipe_ReturnsTheCookedMeal()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("mealplan-api");
		var year = DateTime.UtcNow.Year;
		var week = System.Globalization.ISOWeek.GetWeekOfYear(DateTime.UtcNow);
		var uniqueTitle = $"BrowsableHistoryProbe-{Guid.NewGuid():N}";

		await client.PostAsJsonAsync($"/api/v1/meal-plans/{year}/w/{week}/slots", new
		{
			day = DayOfWeek.Monday,
			recipeIdentifier = Guid.NewGuid(),
			recipeTitle = uniqueTitle,
			mealType = 0,
			servings = 2,
		});
		await client.PutAsJsonAsync($"/api/v1/meal-plans/{year}/w/{week}/slots/confirm", new { day = DayOfWeek.Monday, mealType = 0, state = "Cooked" });

		// Projection is async via Wolverine — poll.
		var deadline = DateTime.UtcNow.AddSeconds(15);
		JsonElement match = default;
		while (DateTime.UtcNow < deadline)
		{
			var response = await client.GetAsync($"/api/v1/meal-plans/history/search?term={Uri.EscapeDataString(uniqueTitle)}");
			var rows = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
			match = rows.EnumerateArray()
				.FirstOrDefault(r => string.Equals(r.GetProperty("recipeTitle").GetString(), uniqueTitle, StringComparison.OrdinalIgnoreCase));
			if (match.ValueKind == JsonValueKind.Object) break;
			await Task.Delay(250);
		}

		match.ValueKind.Should().Be(JsonValueKind.Object);
		match.GetProperty("day").GetString().Should().Be("Monday");
	}
}
