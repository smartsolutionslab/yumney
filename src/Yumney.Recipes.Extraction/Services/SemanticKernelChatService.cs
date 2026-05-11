using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using ChatMessageContent = SmartSolutionsLab.Yumney.Recipes.Domain.Chat.ChatMessageContent;
using ChatRole = SmartSolutionsLab.Yumney.Recipes.Domain.Chat.ChatRole;
using DomainChatHistoryEntry = SmartSolutionsLab.Yumney.Recipes.Domain.Chat.ChatHistoryEntry;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
#pragma warning disable SA1303
public sealed partial class SemanticKernelChatService(
	Kernel kernel,
	SearchRecipesTool searchRecipesTool,
	GetRecipeTool getRecipeTool,
	GetCookableRecipesTool getCookableRecipesTool,
	RateRecipeTool rateRecipeTool,
	GetWeeklyPlanTool getWeeklyPlanTool,
	AssignMealTool assignMealTool,
	ConfirmMealTool confirmMealTool,
	GetMergedShoppingListTool getMergedShoppingListTool,
	CreateShoppingListTool createShoppingListTool,
	AddShoppingItemTool addShoppingItemTool,
	RemoveShoppingItemTool removeShoppingItemTool,
	SwapMealSlotsTool swapMealSlotsTool,
	ClearMealSlotTool clearMealSlotTool,
	ChatToolContext toolContext,
	ILogger<SemanticKernelChatService> logger)
	: IChatService
{
	private const int maxActionsToEmit = 3;

	public async Task<Result<ChatResponseDto>> ChatAsync(
		ChatMessageContent message,
		IReadOnlyList<DomainChatHistoryEntry> history,
		OwnerIdentifier owner,
		CancellationToken cancellationToken = default)
	{
		using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("chat.message");
		activity?.SetTag("chat.history_length", history.Count);

		var chatHistory = BuildChatHistory(message, history);
		var requestKernel = BuildRequestKernel();

		var replyResult = await GetChatReplyAsync(requestKernel, chatHistory, activity, cancellationToken);
		if (replyResult.IsFailure) return replyResult.Error!;

		activity?.SetTag("chat.tool_match_count", toolContext.Matches.Count);
		var suggestions = toolContext.Matches.ToSuggestions();
		var actions = BuildActions(toolContext);
		return new ChatResponseDto(replyResult.Value, suggestions, actions);
	}

	private static List<ChatActionDto> BuildActions(ChatToolContext context)
	{
		List<ChatActionDto> actions = [];
		foreach (var match in context.Matches.Take(maxActionsToEmit))
		{
			actions.Add(new ChatActionDto(ChatActionType.OpenRecipe, RecipeIdentifier: match.Identifier));
		}

		if (context.ProposeStartCookMode && context.Matches.Count > 0)
		{
			actions.Add(new ChatActionDto(ChatActionType.StartCookMode, RecipeIdentifier: context.Matches[0].Identifier));
		}

		return actions;
	}

	private static ChatHistory BuildChatHistory(
		ChatMessageContent message,
		IReadOnlyList<DomainChatHistoryEntry> history)
	{
		var chatHistory = new ChatHistory();
		chatHistory.AddSystemMessage(systemPrompt);

		foreach (var (role, content) in history)
		{
			if (role == ChatRole.User)
			{
				chatHistory.AddUserMessage(content.Value);
			}
			else if (role == ChatRole.Assistant)
			{
				chatHistory.AddAssistantMessage(content.Value);
			}
		}

		chatHistory.AddUserMessage(message.Value);
		return chatHistory;
	}

	private async Task<Result<string>> GetChatReplyAsync(
		Kernel requestKernel,
		ChatHistory chatHistory,
		Activity? activity,
		CancellationToken cancellationToken)
	{
		var chatCompletion = requestKernel.GetRequiredService<IChatCompletionService>();
		var executionSettings = new PromptExecutionSettings
		{
			FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
		};

		try
		{
			var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, executionSettings, requestKernel, cancellationToken);
			var reply = result.Content ?? string.Empty;
			activity?.SetTag("llm.response_length", reply.Length);
			return reply;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			LogChatFailed(ex.Message);
			return ImportRecipeErrors.ExtractionFailed;
		}
	}

	private const string systemPrompt = """
        You are a friendly, concise cooking assistant for the Yumney recipe app.
        You can call functions to look up the user's recipes, pantry, and meal
        plan — prefer calling a function over guessing.

        Tool usage:
        - When the user asks to find or search recipes ("find chicken recipes",
          "what pasta dishes do I have"), call search_recipes.
        - When the user asks what they can cook now or with what's in their
          pantry ("what can I cook?", "I have rice and eggs"), call
          get_cookable_recipes.
        - When the user wants details on a specific recipe (ingredients, steps,
          time), call get_recipe with the identifier from a previous tool call.
        - When the user rates a recipe ("rate the carbonara 5 stars",
          "I'd give that a 4"), call rate_recipe with the identifier from a
          previous tool call and the integer rating.
        - When the user asks about their planned meals ("what's for dinner
          Tuesday?", "show me this week's plan", "was ist nächste Woche
          geplant?"), call get_weekly_plan. Pass year=0 / weekNumber=0 to
          mean "this week".
        - When the user wants to plan a recipe ("plan carbonara for
          Wednesday", "add risotto to Friday lunch"), first resolve the
          recipe identifier via search_recipes if you don't already have
          one, then call assign_meal.
        - When the user reports cooking, skipping, or undoing a planned
          meal ("I made the spaghetti on Wednesday", "skip Friday's
          dinner"), call confirm_meal_cooked with state Cooked/Skipped/
          Planned.
        - When the user asks about their shopping list ("what's on my
          shopping list?", "do I still need eggs?"), call
          get_merged_shopping_list.
        - When the user asks to build a shopping list from recipes
          ("make a list for spaghetti and risotto", "shopping list for
          this week's dinners"), first resolve recipe identifiers via
          search_recipes if needed, then call
          create_shopping_list_from_recipes with the identifiers
          comma-separated.
        - When the user wants to add an item to the shopping list ("add
          milk", "add 2kg potatoes"), call add_to_shopping_list. Pass
          quantity + unit when stated; omit otherwise.
        - When the user wants to remove an item from the shopping list
          ("remove eggs", "I don't need milk anymore"), call
          remove_from_shopping_list.
        - When the user wants to swap two planned meals ("swap Thursday
          and Friday"), call swap_meal_slots.
        - When the user wants to cancel a planned meal ("cancel
          Wednesday", "clear Friday's dinner"), call clear_meal_slot.
        - For general cooking questions or chit-chat, just reply directly
          without calling tools.

        When you've called a tool, mention the most relevant 1-3 recipes by
        title in your reply. Keep replies to 2-3 sentences unless the user
        asks for more detail. The Yumney UI will render the matching recipes
        as buttons under your reply, so do not list URLs or identifiers.

        The user may write in English or German — answer in the same language.
        """;

	[LoggerMessage(Level = LogLevel.Warning, Message = "Chat LLM call failed: {Reason}")]
	private partial void LogChatFailed(string reason);

	private Kernel BuildRequestKernel()
	{
		var requestKernel = kernel.Clone();
		requestKernel.Plugins.AddFromObject(searchRecipesTool, "recipes_search");
		requestKernel.Plugins.AddFromObject(getRecipeTool, "recipes_detail");
		requestKernel.Plugins.AddFromObject(getCookableRecipesTool, "recipes_cookable");
		requestKernel.Plugins.AddFromObject(rateRecipeTool, "recipes_rate");
		requestKernel.Plugins.AddFromObject(getWeeklyPlanTool, "mealplan_weekly");
		requestKernel.Plugins.AddFromObject(assignMealTool, "mealplan_assign");
		requestKernel.Plugins.AddFromObject(confirmMealTool, "mealplan_confirm");
		requestKernel.Plugins.AddFromObject(getMergedShoppingListTool, "shopping_merged");
		requestKernel.Plugins.AddFromObject(createShoppingListTool, "shopping_create");
		requestKernel.Plugins.AddFromObject(addShoppingItemTool, "shopping_add");
		requestKernel.Plugins.AddFromObject(removeShoppingItemTool, "shopping_remove");
		requestKernel.Plugins.AddFromObject(swapMealSlotsTool, "mealplan_swap");
		requestKernel.Plugins.AddFromObject(clearMealSlotTool, "mealplan_clear");
		return requestKernel;
	}
}
