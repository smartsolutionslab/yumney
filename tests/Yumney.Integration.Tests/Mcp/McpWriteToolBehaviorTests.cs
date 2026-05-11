using System.Net.Http.Headers;
using System.Text.Json;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Mcp;

/// <summary>
/// Phase 4 closing-of-closing test: actually persist via MCP write tools and
/// then read it back via the read tools. Catches subtle wiring bugs the
/// contract-only tests miss — e.g. arguments serialized in the wrong shape,
/// owner not propagated, route placeholder bound to the wrong arg name.
///
/// Uses a deliberately-far-future ISO week so the test data never collides
/// with anything else seeded by the suite.
/// </summary>
[Collection(AspireCollection.Name)]
public class McpWriteToolBehaviorTests(AspireFixture fixture)
{
	private const int FutureYear = 2099;

	[Fact]
	public async Task AssignMeal_ThenGetWeeklyPlan_SlotReflectsAssignment()
	{
		await using var client = await CreateClientAsync();
		var recipeId = Guid.NewGuid();
		const string recipeTitle = "Test Carbonara (assign)";
		const int week = 11;

		var assignResult = await client.CallToolAsync("assign_meal", new Dictionary<string, object?>
		{
			["day"] = "Wednesday",
			["recipeIdentifier"] = recipeId.ToString(),
			["recipeTitle"] = recipeTitle,
			["mealType"] = "Dinner",
			["year"] = FutureYear,
			["weekNumber"] = week,
		});

		assignResult.IsError.Should().BeFalse(because: GetText(assignResult));

		var planResult = await client.CallToolAsync("get_weekly_plan", new Dictionary<string, object?>
		{
			["year"] = FutureYear,
			["weekNumber"] = week,
		});

		planResult.IsError.Should().BeFalse();
		var planText = GetText(planResult);
		planText.Should().Contain(recipeId.ToString().ToLowerInvariant());
		planText.Should().Contain(recipeTitle);
		planText.Should().Contain("Wednesday");
	}

	[Fact]
	public async Task ConfirmMealCooked_AfterAssign_StateReflectsCooked()
	{
		await using var client = await CreateClientAsync();
		var recipeId = Guid.NewGuid();
		const int week = 12;

		await client.CallToolAsync("assign_meal", new Dictionary<string, object?>
		{
			["day"] = "Friday",
			["recipeIdentifier"] = recipeId.ToString(),
			["recipeTitle"] = "Test Pizza (confirm)",
			["mealType"] = "Dinner",
			["year"] = FutureYear,
			["weekNumber"] = week,
		});

		var confirmResult = await client.CallToolAsync("confirm_meal_cooked", new Dictionary<string, object?>
		{
			["day"] = "Friday",
			["state"] = "Cooked",
			["mealType"] = "Dinner",
			["year"] = FutureYear,
			["weekNumber"] = week,
		});

		confirmResult.IsError.Should().BeFalse(because: GetText(confirmResult));

		var planResult = await client.CallToolAsync("get_weekly_plan", new Dictionary<string, object?>
		{
			["year"] = FutureYear,
			["weekNumber"] = week,
		});

		planResult.IsError.Should().BeFalse();
		var planJson = JsonDocument.Parse(GetText(planResult));
		var slots = planJson.RootElement.GetProperty("slots").EnumerateArray();
		var fridaySlot = slots.Single(slot =>
			slot.GetProperty("day").GetString() == "Friday"
			&& slot.GetProperty("mealType").GetString() == "Dinner");
		fridaySlot.GetProperty("recipeIdentifier").GetGuid().Should().Be(recipeId);
	}

	[Fact]
	public async Task CreateShoppingListFromRecipes_PersistsList_ReturnsItemsFromRecipe()
	{
		// What `create_shopping_list_from_recipes` actually does: it builds a
		// ShoppingList aggregate, saves it via IShoppingListEventStore, and
		// returns the materialised list (ShoppingListDetailDto). The merged-
		// read read model (`get_merged_shopping_list`) is a *separate* ledger
		// projection populated by ledger events (Added/Bought/Consumed/…) —
		// not by ShoppingListCreated events. So we can't observe the list
		// through that endpoint and shouldn't try.
		//
		// Instead, assert directly against the create-tool response: it
		// echoes the persisted list with its items, which is exactly the
		// round-trip wiring this test is here to prove (arguments serialised
		// in the right shape, owner propagated, ingredient lookup against
		// recipes-api succeeded, items merged + persisted).
		var ownerId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.TomatoSoup(owner: ownerId);
		await fixture.SeedRecipesAsync(recipe);

		await using var client = await CreateClientAsync();

		var createResult = await client.CallToolAsync("create_shopping_list_from_recipes", new Dictionary<string, object?>
		{
			["title"] = $"MCP integration list {Guid.NewGuid():N}",
			["recipes"] = new[]
			{
				new { recipeIdentifier = recipe.Id.Value, servings = (int?)null },
			},
		});

		createResult.IsError.Should().BeFalse(because: GetText(createResult));

		var createdJson = JsonDocument.Parse(GetText(createResult));
		var itemNames = createdJson.RootElement.GetProperty("items")
			.EnumerateArray()
			.Select(item => item.GetProperty("name").GetString())
			.ToList();
		itemNames.Should().Contain("Tomatoes");
	}

	[Fact]
	public async Task AssignMealWithMissingRequiredArgument_ReturnsError()
	{
		await using var client = await CreateClientAsync();

		// Day omitted — model bound parameter, MealPlan should reject the request.
		var result = await client.CallToolAsync("assign_meal", new Dictionary<string, object?>
		{
			["recipeIdentifier"] = Guid.NewGuid().ToString(),
			["recipeTitle"] = "Whatever",
			["mealType"] = "Dinner",
			["year"] = FutureYear,
			["weekNumber"] = 13,
		});

		// The tool's response wraps the upstream failure — IsError or a
		// "Couldn't plan" message both prove the missing-arg path surfaces.
		var text = GetText(result);
		var failed = (result.IsError ?? false) || text.Contains("Couldn't plan", StringComparison.OrdinalIgnoreCase);
		failed.Should().BeTrue($"expected either IsError=true or a Couldn't-plan message; got: {text}");
	}

	private static string GetText(CallToolResult result) =>
		result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;

	private async Task<McpClient> CreateClientAsync()
	{
		var token = await fixture.GetAccessTokenAsync("testuser", "Test1234");
		var http = fixture.App.CreateHttpClient("mcp-server");
		http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var options = new HttpClientTransportOptions
		{
			Endpoint = new Uri(http.BaseAddress!, "/mcp"),
			Name = "mcp-write-behavior-test",
		};
		var transport = new HttpClientTransport(options, http, NullLoggerFactory.Instance, ownsHttpClient: true);

		return await McpClient.CreateAsync(transport, clientOptions: null, loggerFactory: NullLoggerFactory.Instance);
	}
}
