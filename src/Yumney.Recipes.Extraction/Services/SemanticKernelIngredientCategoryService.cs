using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public sealed partial class SemanticKernelIngredientCategoryService(
    Kernel kernel,
    ILogger<SemanticKernelIngredientCategoryService> logger) : IIngredientCategoryService
{
    private const string SystemPrompt = """
        You are a grocery item categorizer. Given an item name, respond with exactly one of these categories:
        produce, dairy, meat-fish, bakery, frozen, beverages, pantry, household, other

        Respond with only the category value, nothing else. No explanation, no punctuation.
        The item may be in English or German.

        Examples:
        "tahini" → pantry
        "Tofu" → produce
        "Alufolie" → household
        "Tiefkühlerbsen" → frozen
        """;

    public async Task<IngredientCategory> CategorizeAsync(string itemName, CancellationToken cancellationToken = default)
    {
        var staticResult = IngredientCategoryResolver.Resolve(itemName);
        if (staticResult is not null)
            return staticResult;

        try
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(SystemPrompt);
            chatHistory.AddUserMessage(itemName);

            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            var response = result.Content?.Trim().ToLowerInvariant() ?? string.Empty;

            return ParseCategory(response, itemName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogCategorizationFailed(itemName, ex.Message);
            return IngredientCategory.Other;
        }
    }

    private IngredientCategory ParseCategory(string response, string itemName)
    {
        try
        {
            return IngredientCategory.From(response);
        }
        catch
        {
            LogUnrecognizedCategory(itemName, response);
            return IngredientCategory.Other;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "LLM categorization failed for '{ItemName}': {Reason}")]
    private partial void LogCategorizationFailed(string itemName, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "LLM returned unrecognized category for '{ItemName}': '{Response}'")]
    private partial void LogUnrecognizedCategory(string itemName, string response);
}
