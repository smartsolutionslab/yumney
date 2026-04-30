using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using ChatMessageContent = SmartSolutionsLab.Yumney.Recipes.Domain.Chat.ChatMessageContent;
using ChatRole = SmartSolutionsLab.Yumney.Recipes.Domain.Chat.ChatRole;
using DomainChatHistoryEntry = SmartSolutionsLab.Yumney.Recipes.Domain.Chat.ChatHistoryEntry;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
#pragma warning disable SA1311
#pragma warning disable SA1204
#pragma warning disable SA1303
public sealed partial class SemanticKernelChatService(Kernel kernel, IRecipeRepository recipes, ILogger<SemanticKernelChatService> logger)
	: IChatService
{
	private const int maxRecipesToInclude = 20;

	public async Task<Result<ChatResponseDto>> ChatAsync(
		ChatMessageContent message,
		IReadOnlyList<DomainChatHistoryEntry> history,
		OwnerIdentifier owner,
		CancellationToken cancellationToken = default)
	{
		using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("chat.message");
		activity?.SetTag("chat.history_length", history.Count);

		var userRecipes = await LoadUserRecipeContextAsync(owner, cancellationToken);

		var chatHistory = new ChatHistory();
		chatHistory.AddSystemMessage(BuildSystemPrompt(userRecipes));

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

		var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

		string reply;
		try
		{
			var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
			reply = result.Content ?? string.Empty;
			activity?.SetTag("llm.response_length", reply.Length);
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

		var suggestions = MatchRecipesByMention(reply, userRecipes);

		return new ChatResponseDto(reply, suggestions);
	}

	internal static List<ChatRecipeSuggestionDto> MatchRecipesByMention(string reply, IReadOnlyList<Recipe> userRecipes)
	{
		var suggestions = new List<ChatRecipeSuggestionDto>();

		foreach (var recipe in userRecipes)
		{
			var title = recipe.Title.Value;
			var pattern = $@"(?<!\w){Regex.Escape(title)}(?!\w)";

			if (Regex.IsMatch(reply, pattern, RegexOptions.IgnoreCase))
			{
				suggestions.Add(new ChatRecipeSuggestionDto(
					recipe.Id.Value,
					title,
					Reason: null));
			}
		}

		return suggestions;
	}

	private static string BuildSystemPrompt(IReadOnlyList<Recipe> userRecipes)
	{
		var recipeList = userRecipes.Count == 0
			? "(no recipes yet)"
			: string.Join("\n", userRecipes.Select(recipe => $"- {recipe.Title.Value}"));

		return $$"""
            You are a friendly, concise cooking assistant for the Yumney recipe app.
            Help the user discover recipes from their collection or suggest new ones.
            When suggesting recipes from their collection, mention the exact title in quotes.
            Keep answers to 2-3 sentences unless the user asks for more detail.

            The user's recipe collection contains:
            {{recipeList}}

            If the user asks for something not in their collection, suggest a general recipe
            idea or recommend they import one from a website.
            """;
	}

	private async Task<IReadOnlyList<Recipe>> LoadUserRecipeContextAsync(OwnerIdentifier owner, CancellationToken cancellationToken)
	{
		try
		{
			var paging = PagingOptions.Of(Page.From(1), PageSize.From(maxRecipesToInclude));
			var sorting = new SortingOptions<RecipeSortField>(RecipeSortField.Date, SortDirection.Descending);

			var (items, _) = await recipes.GetByOwnerAsync(
				owner,
				paging,
				sorting,
				search: null,
				filter: null,
				cancellationToken: cancellationToken);
			return items;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogRecipeContextLoadFailed(ex.Message);
			return [];
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Chat LLM call failed: {Reason}")]
	private partial void LogChatFailed(string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load recipe context for chat: {Reason}")]
	private partial void LogRecipeContextLoadFailed(string reason);
}
