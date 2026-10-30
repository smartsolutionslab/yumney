using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.TestBuilders.Llm;
using Xunit;
using DomainChat = SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

/// <summary>
/// Drives the real chat service with real tools under MEAI's function-invoking
/// loop; only the model is scripted (<see cref="ToolCallingChatClient"/>) and
/// the handlers behind the tools are substituted. Covers tool wiring, the
/// <see cref="ChatToolContext"/> aggregation and the derived suggestions/actions.
/// </summary>
#pragma warning disable SA1601
public sealed partial class LlmChatServiceTests
#pragma warning restore SA1601
{
	private readonly IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>> searchHandler =
		Substitute.For<IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>>();

	private readonly IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>> getHandler =
		Substitute.For<IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>>>();

	private readonly IQueryHandler<GetCookableRecipesQuery, Result<PagedResult<CookableRecipeDto>>> cookableHandler =
		Substitute.For<IQueryHandler<GetCookableRecipesQuery, Result<PagedResult<CookableRecipeDto>>>>();

	[Fact]
	public async Task ChatAsync_LlmInvokesSearchRecipes_ReturnsSuggestionsAndOpenRecipeActions()
	{
		var firstId = Guid.NewGuid();
		var secondId = Guid.NewGuid();
		searchHandler.HandleAsync(Arg.Any<GetRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(SuccessRecipes(firstId, "Carbonara", secondId, "Risotto"));

		var fake = new ToolCallingChatClient(
			ToolCallingChatClient.Turn.Call("search_recipes", new() { ["query"] = "pasta" }),
			ToolCallingChatClient.Turn.Reply("I found 2 recipes matching your search."));

		var (chatService, context) = BuildChatService(fake);

		var result = await chatService.ChatAsync(
			DomainChat.ChatMessageContent.From("show me pasta recipes"),
			[],
			Owner());

		result.IsSuccess.Should().BeTrue();
		result.Value.Reply.Should().Contain("2 recipes");
		result.Value.Suggestions.Should().HaveCount(2);
		result.Value.Actions.Should().HaveCount(2);
		result.Value.Actions.Should().AllSatisfy(action => action.Type.Should().Be(ChatActionType.OpenRecipe));
		result.Value.Actions.Select(action => action.RecipeIdentifier).Should().Contain([firstId, secondId]);
		context.Matches.Should().HaveCount(2);
		await searchHandler.Received(1).HandleAsync(Arg.Any<GetRecipesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ChatAsync_LlmInvokesGetCookableRecipes_EmitsStartCookModeAction()
	{
		var id = Guid.NewGuid();
		cookableHandler.HandleAsync(Arg.Any<GetCookableRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(SuccessCookable(id, "Stew", CookableRecipeMatchTier.Full));

		var fake = new ToolCallingChatClient(
			ToolCallingChatClient.Turn.Call("get_cookable_recipes", new() { ["fullMatchOnly"] = false }),
			ToolCallingChatClient.Turn.Reply("You can cook Stew right now."));

		var (chatService, context) = BuildChatService(fake);

		var result = await chatService.ChatAsync(
			DomainChat.ChatMessageContent.From("what can I cook?"),
			[],
			Owner());

		result.IsSuccess.Should().BeTrue();
		context.ProposeStartCookMode.Should().BeTrue();
		result.Value.Actions.Should().HaveCount(2);
		result.Value.Actions[0].Type.Should().Be(ChatActionType.OpenRecipe);
		result.Value.Actions[0].RecipeIdentifier.Should().Be(id);
		result.Value.Actions[1].Type.Should().Be(ChatActionType.StartCookMode);
		result.Value.Actions[1].RecipeIdentifier.Should().Be(id);
	}

	[Fact]
	public async Task ChatAsync_LlmReturnsTextOnly_NoTools_NoActionsOrSuggestions()
	{
		var fake = new ToolCallingChatClient(ToolCallingChatClient.Turn.Reply("Bring water to a simmer, then crack the egg in."));

		var (chatService, context) = BuildChatService(fake);

		var result = await chatService.ChatAsync(
			DomainChat.ChatMessageContent.From("how do I poach an egg?"),
			[],
			Owner());

		result.IsSuccess.Should().BeTrue();
		result.Value.Reply.Should().Contain("simmer");
		result.Value.Suggestions.Should().BeEmpty();
		result.Value.Actions.Should().BeEmpty();
		context.Matches.Should().BeEmpty();
		await searchHandler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
	}

	[Fact]
	public async Task ChatAsync_LlmInvokesGetRecipe_AppendsContextAndOpenRecipeAction()
	{
		var id = Guid.NewGuid();
		getHandler.HandleAsync(Arg.Any<GetRecipeByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(SuccessRecipeDetail(id, "Detailed Carbonara"));

		var fake = new ToolCallingChatClient(
			ToolCallingChatClient.Turn.Call("get_recipe", new() { ["recipeIdentifier"] = id.ToString() }),
			ToolCallingChatClient.Turn.Reply("Here's the recipe."));

		var (chatService, context) = BuildChatService(fake);

		var result = await chatService.ChatAsync(
			DomainChat.ChatMessageContent.From("show me the carbonara details"),
			[],
			Owner());

		result.IsSuccess.Should().BeTrue();
		context.Matches.Should().ContainSingle();
		context.Matches[0].Identifier.Should().Be(id);
		result.Value.Actions.Should().ContainSingle();
		result.Value.Actions[0].Type.Should().Be(ChatActionType.OpenRecipe);
		result.Value.Actions[0].RecipeIdentifier.Should().Be(id);
	}

	[Fact]
	public async Task ChatAsync_LlmCallsTwoToolsInSequence_BothMatchesAggregated()
	{
		var firstId = Guid.NewGuid();
		var secondId = Guid.NewGuid();
		searchHandler.HandleAsync(Arg.Any<GetRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(SuccessRecipes(firstId, "Carbonara"));
		getHandler.HandleAsync(Arg.Any<GetRecipeByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(SuccessRecipeDetail(secondId, "Risotto"));

		var fake = new ToolCallingChatClient(
			ToolCallingChatClient.Turn.CallAll(
				("search_recipes", new() { ["query"] = "pasta" }),
				("get_recipe", new() { ["recipeIdentifier"] = secondId.ToString() })),
			ToolCallingChatClient.Turn.Reply("Here are some options."));

		var (chatService, context) = BuildChatService(fake);

		var result = await chatService.ChatAsync(
			DomainChat.ChatMessageContent.From("find pasta then show me one in detail"),
			[],
			Owner());

		result.IsSuccess.Should().BeTrue();
		context.Matches.Should().HaveCount(2);
		context.Matches.Select(match => match.Identifier).Should().Contain([firstId, secondId]);
	}

	private static OwnerIdentifier Owner() => OwnerIdentifier.From("test-user");

	private (LlmChatService Service, ChatToolContext Context) BuildChatService(ToolCallingChatClient fake)
	{
		var context = new ChatToolContext();
		var service = new LlmChatService(fake, BuildTools(context), context, NullLogger<LlmChatService>.Instance);
		return (service, context);
	}

	private List<IChatTool> BuildTools(ChatToolContext context) =>
	[
		new SearchRecipesTool(searchHandler, context),
		new GetRecipeTool(getHandler, context),
		new GetCookableRecipesTool(cookableHandler, context),
		new RateRecipeTool(Substitute.For<ICommandHandler<RateRecipeCommand, Result>>()),
		new GetWeeklyPlanTool(Substitute.For<IWeeklyPlanLookup>(), context),
		new AssignMealTool(Substitute.For<IMealPlanScheduler>(), context),
		new ConfirmMealTool(Substitute.For<IMealConfirmation>()),
		new GetMergedShoppingListTool(Substitute.For<IShoppingListLookup>()),
		new CreateShoppingListTool(Substitute.For<IShoppingListCreator>()),
		new AddShoppingItemTool(Substitute.For<IShoppingListItemAdder>()),
		new RemoveShoppingItemTool(Substitute.For<IShoppingListItemRemover>()),
		new SwapMealSlotsTool(Substitute.For<IMealSlotSwapper>()),
		new ClearMealSlotTool(Substitute.For<IMealSlotClearer>()),
	];
}
