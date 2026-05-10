using System.Net.Http.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes;

/// <summary>
/// Phase 1 (#642) coverage: the chat endpoint emits ChatResponseDto.Actions[]
/// driven by the intent parser. Verifies the HTTP contract end-to-end —
/// request through Keycloak auth, command handler, intent parser, mapper, and
/// back through the Result→IResult pipeline.
///
/// Relies on StubIntentParserService recognising specific navigate phrases
/// deterministically (E2ETests=true mode); a real LLM is not invoked.
/// </summary>
[Collection(AspireCollection.Name)]
public class RecipesChatActionsContractTests(AspireFixture fixture)
{
	[Fact]
	public async Task Chat_OpenShoppingListMessage_ReturnsNavigateActionToShopping()
	{
		var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		var request = new ChatRequestDto("open shopping list", []);

		var response = await client.PostAsJsonAsync("/api/v1/recipes/chat", request);

		response.IsSuccessStatusCode.Should().BeTrue();
		var dto = await response.Content.ReadFromJsonAsync<ChatResponseDto>();
		dto.Should().NotBeNull();
		dto!.Actions.Should().ContainSingle();
		dto.Actions[0].Type.Should().Be(ChatActionType.Navigate);
		dto.Actions[0].Route.Should().Be("/shopping");
	}

	[Fact]
	public async Task Chat_OpenMealPlannerMessage_ReturnsNavigateActionToMealPlanner()
	{
		var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		var request = new ChatRequestDto("open meal planner please", []);

		var response = await client.PostAsJsonAsync("/api/v1/recipes/chat", request);

		response.IsSuccessStatusCode.Should().BeTrue();
		var dto = await response.Content.ReadFromJsonAsync<ChatResponseDto>();
		dto!.Actions.Should().ContainSingle();
		dto.Actions[0].Route.Should().Be("/meal-planner");
	}

	[Fact]
	public async Task Chat_GenericQuestion_ReturnsEmptyActions()
	{
		var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		var request = new ChatRequestDto("what's a good way to poach an egg?", []);

		var response = await client.PostAsJsonAsync("/api/v1/recipes/chat", request);

		response.IsSuccessStatusCode.Should().BeTrue();
		var dto = await response.Content.ReadFromJsonAsync<ChatResponseDto>();
		dto!.Actions.Should().BeEmpty();
	}

	[Fact]
	public async Task Chat_WithoutBearer_IsUnauthorized()
	{
		var request = new ChatRequestDto("anything", []);

		var response = await fixture.RecipesApi.PostAsJsonAsync("/api/v1/recipes/chat", request);

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
	}
}
