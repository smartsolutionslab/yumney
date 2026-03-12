using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

#pragma warning disable SA1601
#pragma warning disable SA1303
#pragma warning disable SA1311
public sealed partial class SemanticKernelRecipeExtractionService(Kernel kernel, ILogger<SemanticKernelRecipeExtractionService> logger)
    : IRecipeExtractionService
{
    private const string llmNoRecipeErrorCode = "NO_RECIPE_FOUND";
    private const string errorPropertyName = "error";
    private const string jsonFencePrefix = "```json";
    private const string fenceMarker = "```";

    private const string systemPrompt = $$"""
        You are a recipe extraction assistant. Extract structured recipe data
        from the provided webpage content. Respond ONLY with valid JSON matching this schema:
        {
          "title": "string (required)",
          "description": "string or null",
          "ingredients": [{ "name": "string", "amount": number or null, "unit": "string or null" }],
          "steps": [{ "number": integer, "description": "string" }],
          "servings": integer or null,
          "prepTimeMinutes": integer or null,
          "cookTimeMinutes": integer or null,
          "difficulty": "easy" | "medium" | "hard" or null,
          "imageUrl": "string or null"
        }
        If the content does not contain a recipe, respond with: { "{{errorPropertyName}}": "{{llmNoRecipeErrorCode}}" }
        """;

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result<ExtractedRecipeDto>> ExtractAsync(ScrapedContent content, CancellationToken cancellationToken = default)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(content.CleanedText);

        string response;
        try
        {
            var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            response = result.Content ?? string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogLlmCallFailed(content.SourceUrl.Value, ex.Message);
            return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.ExtractionFailed);
        }

        return ParseResponse(response, content.SourceUrl.Value);
    }

    private static string ExtractJson(string response)
    {
        var trimmed = response.Trim();

        if (trimmed.StartsWith(jsonFencePrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[jsonFencePrefix.Length..];
        }
        else if (trimmed.StartsWith(fenceMarker, StringComparison.Ordinal))
        {
            trimmed = trimmed[fenceMarker.Length..];
        }

        if (trimmed.EndsWith(fenceMarker, StringComparison.Ordinal))
        {
            trimmed = trimmed[..^fenceMarker.Length];
        }

        return trimmed.Trim();
    }

    private Result<ExtractedRecipeDto> ParseResponse(string response, string sourceUrl)
    {
        try
        {
            var json = ExtractJson(response);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty(errorPropertyName, out var errorElement)
                && errorElement.GetString() == llmNoRecipeErrorCode)
            {
                LogNoRecipeFound(sourceUrl);
                return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.NoRecipeFound);
            }

            var recipe = root.Deserialize<ExtractedRecipeDto>(jsonOptions);
            if (recipe is null)
            {
                LogParsingFailed(sourceUrl, "Deserialization returned null");
                return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.ExtractionFailed);
            }

            return Result<ExtractedRecipeDto>.Success(recipe);
        }
        catch (JsonException ex)
        {
            LogParsingFailed(sourceUrl, ex.Message);
            return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.ExtractionFailed);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "LLM call failed for URL {SourceUrl}: {Reason}")]
    private partial void LogLlmCallFailed(string sourceUrl, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No recipe found in content from URL {SourceUrl}")]
    private partial void LogNoRecipeFound(string sourceUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse LLM response for URL {SourceUrl}: {Reason}")]
    private partial void LogParsingFailed(string sourceUrl, string reason);
}
