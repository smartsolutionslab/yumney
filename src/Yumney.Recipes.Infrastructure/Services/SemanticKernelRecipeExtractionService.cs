using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Yumney.Recipes.Application.Commands;
using Yumney.Recipes.Application.DTOs;
using Yumney.Recipes.Application.Interfaces;
using Yumney.Shared.Common;

namespace Yumney.Recipes.Infrastructure.Services;

#pragma warning disable SA1601
#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter (editorconfig requires camelCase for private fields)
public sealed partial class SemanticKernelRecipeExtractionService(
    Kernel kernel,
    ILogger<SemanticKernelRecipeExtractionService> logger)
    : IRecipeExtractionService
{
    private const string SystemPrompt = """
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
        If the content does not contain a recipe, respond with: { "error": "NO_RECIPE_FOUND" }
        """;

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result<ExtractedRecipeDto>> ExtractAsync(
        ScrapedContent content, CancellationToken cancellationToken = default)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        chatHistory.AddUserMessage(content.CleanedText);

        string response;
        try
        {
            var result = await chatCompletion.GetChatMessageContentAsync(
                chatHistory, cancellationToken: cancellationToken);
            response = result.Content ?? string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogLlmCallFailed(content.SourceUrl, ex.Message);
            return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.ExtractionFailed);
        }

        return ParseResponse(response, content.SourceUrl);
    }

    private static string ExtractJson(string response)
    {
        var trimmed = response.Trim();

        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["```json".Length..];
        }
        else if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed["```".Length..];
        }

        if (trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^"```".Length];
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

            if (root.TryGetProperty("error", out var errorElement)
                && errorElement.GetString() == "NO_RECIPE_FOUND")
            {
                LogNoRecipeFound(sourceUrl);
                return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.NoRecipeFound);
            }

            var recipe = JsonSerializer.Deserialize<ExtractedRecipeDto>(json, jsonOptions);
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

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "LLM call failed for URL {SourceUrl}: {Reason}")]
    private partial void LogLlmCallFailed(string sourceUrl, string reason);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No recipe found in content from URL {SourceUrl}")]
    private partial void LogNoRecipeFound(string sourceUrl);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to parse LLM response for URL {SourceUrl}: {Reason}")]
    private partial void LogParsingFailed(string sourceUrl, string reason);
}
