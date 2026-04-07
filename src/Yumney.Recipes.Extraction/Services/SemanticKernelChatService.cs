using System.Diagnostics;
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

        foreach (var entry in history)
        {
            if (entry.Role == ChatRole.User)
            {
                chatHistory.AddUserMessage(entry.Content.Value);
            }
            else if (entry.Role == ChatRole.Assistant)
            {
                chatHistory.AddAssistantMessage(entry.Content.Value);
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

    private static string BuildSystemPrompt(IReadOnlyList<Recipe> userRecipes)
    {
        var recipeList = userRecipes.Count == 0
            ? "(no recipes yet)"
            : string.Join("\n", userRecipes.Select(r => $"- {r.Title.Value}"));

        return $$"""
            You are a friendly cooking assistant for the Yumney recipe app.
            Help the user discover recipes from their collection or suggest new ones.
            When suggesting recipes from their collection, mention the exact title in quotes.

            The user's recipe collection contains:
            {{recipeList}}

            Be concise. Use natural language. If the user asks for something not in their
            collection, suggest a general recipe idea or recommend they import one from a website.
            """;
    }

    private static List<ChatRecipeSuggestionDto> MatchRecipesByMention(
        string reply,
        IReadOnlyList<Recipe> userRecipes)
    {
        var suggestions = new List<ChatRecipeSuggestionDto>();

        foreach (var recipe in userRecipes)
        {
            if (reply.Contains(recipe.Title.Value, StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new ChatRecipeSuggestionDto(
                    recipe.Id.Value,
                    recipe.Title.Value,
                    Reason: null));
            }
        }

        return suggestions;
    }

    private async Task<IReadOnlyList<Recipe>> LoadUserRecipeContextAsync(OwnerIdentifier owner, CancellationToken cancellationToken)
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Chat LLM call failed: {Reason}")]
    private partial void LogChatFailed(string reason);
}
