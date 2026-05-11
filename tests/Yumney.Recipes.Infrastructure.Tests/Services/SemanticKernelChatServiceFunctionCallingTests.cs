using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;
using DomainChat = SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

/// <summary>
/// Phase 2a (#643) coverage gap: the SK function-calling integration. Unit
/// tests for individual tools verify they map handler results into chat
/// shapes; this test wires real <see cref="Kernel"/> + real tool plugin
/// registration + a fake <see cref="IChatCompletionService"/> that emits
/// <see cref="FunctionCallContent"/>, then asserts SK's auto-invoke loop
/// dispatches to the right tool and the chat service derives the expected
/// suggestions / actions from <see cref="ChatToolContext"/>.
///
/// Handlers are NSubstitute'd — this test is about the SK plumbing, not the
/// handler logic (which the Application.Tests cover).
/// </summary>
public sealed class SemanticKernelChatServiceFunctionCallingTests
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

		var fake = new FakeChatCompletionService(
			[BuildFunctionCall("search_recipes", "recipes_search", new() { ["query"] = "pasta" })],
			"I found 2 recipes matching your search.");

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

		var fake = new FakeChatCompletionService(
			[BuildFunctionCall("get_cookable_recipes", "recipes_cookable", new() { ["fullMatchOnly"] = false })],
			"You can cook Stew right now.");

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
		var fake = new FakeChatCompletionService([], "Bring water to a simmer, then crack the egg in.");

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

		var fake = new FakeChatCompletionService(
			[BuildFunctionCall("get_recipe", "recipes_detail", new() { ["recipeIdentifier"] = id.ToString() })],
			"Here's the recipe.");

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

		var fake = new FakeChatCompletionService(
			[
				BuildFunctionCall("search_recipes", "recipes_search", new() { ["query"] = "pasta" }),
				BuildFunctionCall("get_recipe", "recipes_detail", new() { ["recipeIdentifier"] = secondId.ToString() }),
			],
			"Here are some options.");

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

	private static FunctionCallContent BuildFunctionCall(string functionName, string pluginName, KernelArguments arguments)
	{
		var callId = Guid.NewGuid().ToString();
		return new FunctionCallContent(functionName, pluginName, callId, arguments);
	}

	private static Result<PagedResult<RecipeListItemDto>> SuccessRecipes(params object[] idTitlePairs)
	{
		List<RecipeListItemDto> items = [];
		for (var index = 0; index < idTitlePairs.Length; index += 2)
		{
			var id = (Guid)idTitlePairs[index];
			var title = (string)idTitlePairs[index + 1];
			items.Add(new RecipeListItemDto(
				id,
				title,
				Description: null,
				Servings: 4,
				PrepTimeMinutes: 10,
				CookTimeMinutes: 20,
				Difficulty: null,
				ImageUrl: null,
				CreatedAt: DateTime.UtcNow,
				Tags: [],
				IsFavorite: false,
				Rating: null,
				HasNotes: false));
		}

		return Result<PagedResult<RecipeListItemDto>>.Success(new PagedResult<RecipeListItemDto>(
			items,
			TotalCount: items.Count,
			Page: 1,
			PageSize: 10));
	}

	private static Result<PagedResult<CookableRecipeDto>> SuccessCookable(Guid id, string title, CookableRecipeMatchTier tier)
	{
		var item = new CookableRecipeDto(
			id,
			title,
			ImageUrl: null,
			Servings: 4,
			PrepTimeMinutes: 10,
			CookTimeMinutes: 20,
			Difficulty: null,
			IngredientCount: 6,
			Tier: tier,
			MissingIngredients: []);
		return Result<PagedResult<CookableRecipeDto>>.Success(new PagedResult<CookableRecipeDto>(
			[item],
			TotalCount: 1,
			Page: 1,
			PageSize: 5));
	}

	private static Result<RecipeDetailDto> SuccessRecipeDetail(Guid id, string title) =>
		Result<RecipeDetailDto>.Success(new RecipeDetailDto(
			id,
			title,
			Description: null,
			Servings: 4,
			PrepTimeMinutes: 10,
			CookTimeMinutes: 20,
			Difficulty: null,
			ImageUrl: null,
			Language: null,
			SourceUrl: null,
			CreatedAt: DateTime.UtcNow,
			Ingredients: [],
			Steps: [],
			Tags: [],
			IsFavorite: false,
			Rating: null,
			Notes: null));

	private (SemanticKernelChatService Service, ChatToolContext Context) BuildChatService(FakeChatCompletionService fake)
	{
		var context = new ChatToolContext();
		var searchTool = new SearchRecipesTool(searchHandler, context);
		var getTool = new GetRecipeTool(getHandler, context);
		var cookableTool = new GetCookableRecipesTool(cookableHandler, context);
		var rateTool = new RateRecipeTool(Substitute.For<ICommandHandler<RateRecipeCommand, Result>>());
		var weeklyPlanTool = new GetWeeklyPlanTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IWeeklyPlanLookup>(),
			context);
		var assignMealTool = new AssignMealTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IMealPlanScheduler>(),
			context);
		var confirmMealTool = new ConfirmMealTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IMealConfirmation>());
		var mergedShoppingListTool = new GetMergedShoppingListTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IShoppingListLookup>());
		var createShoppingListTool = new CreateShoppingListTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IShoppingListCreator>());
		var addShoppingItemTool = new AddShoppingItemTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IShoppingListItemAdder>());
		var removeShoppingItemTool = new RemoveShoppingItemTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IShoppingListItemRemover>());
		var swapMealSlotsTool = new SwapMealSlotsTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IMealSlotSwapper>());
		var clearMealSlotTool = new ClearMealSlotTool(
			Substitute.For<SmartSolutionsLab.Yumney.Recipes.Application.Interfaces.IMealSlotClearer>());

		var kernelBuilder = Kernel.CreateBuilder();
		kernelBuilder.Services.AddSingleton<IChatCompletionService>(fake);
		var kernel = kernelBuilder.Build();

		var service = new SemanticKernelChatService(
			kernel,
			searchTool,
			getTool,
			cookableTool,
			rateTool,
			weeklyPlanTool,
			assignMealTool,
			confirmMealTool,
			mergedShoppingListTool,
			createShoppingListTool,
			addShoppingItemTool,
			removeShoppingItemTool,
			swapMealSlotsTool,
			clearMealSlotTool,
			context,
			NullLogger<SemanticKernelChatService>.Instance);

		return (service, context);
	}

	/// <summary>
	/// Fake IChatCompletionService that emulates an LLM-with-function-calling
	/// in a single round trip: invoke the requested kernel functions itself
	/// (populating ChatToolContext via the real tool methods) and return the
	/// canned final reply. SK's auto-invoke loop is implemented inside each
	/// connector (OpenAI/Anthropic/Azure), not in the abstract; bypassing it
	/// here is intentional — what we test is OUR plumbing (tool registration,
	/// ChatToolContext aggregation, action emission), not the SDK's loop.
	/// </summary>
	private sealed class FakeChatCompletionService(
		IReadOnlyList<FunctionCallContent> functionCallsToInvoke,
		string finalReply) : IChatCompletionService
	{
		public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

		public async Task<IReadOnlyList<Microsoft.SemanticKernel.ChatMessageContent>> GetChatMessageContentsAsync(
			ChatHistory chatHistory,
			PromptExecutionSettings? executionSettings = null,
			Kernel? kernel = null,
			CancellationToken cancellationToken = default)
		{
			if (kernel is not null)
			{
				foreach (var call in functionCallsToInvoke)
				{
					var function = kernel.Plugins[call.PluginName!][call.FunctionName];
					await function.InvokeAsync(kernel, call.Arguments, cancellationToken);
				}
			}

			return [new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.Assistant, finalReply)];
		}

		public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
			ChatHistory chatHistory,
			PromptExecutionSettings? executionSettings = null,
			Kernel? kernel = null,
			CancellationToken cancellationToken = default) =>
			throw new NotImplementedException("Streaming not used by SemanticKernelChatService.");
	}
}
