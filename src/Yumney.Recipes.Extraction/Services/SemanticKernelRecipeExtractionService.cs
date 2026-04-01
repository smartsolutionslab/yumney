using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
#pragma warning disable SA1311
public sealed partial class SemanticKernelRecipeExtractionService(Kernel kernel, ILogger<SemanticKernelRecipeExtractionService> logger)
    : IRecipeExtractionService
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result<ExtractedRecipeDto>> ExtractAsync(ScrapedContent content, CancellationToken cancellationToken = default)
    {
        using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("extract.recipe.url");
        activity?.SetTag("extract.source", content.SourceUrl.Value);
        activity?.SetTag("extract.content_length", content.CleanedText.Length);

        var sanitized = ContentSanitizer.Sanitize(content.CleanedText);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(ExtractionPrompts.WebExtraction);
        chatHistory.AddUserMessage(ExtractionPrompts.WrapInContentDelimiters(sanitized));

        var result = await CallLlmAndParseAsync(chatHistory, content.SourceUrl.Value, cancellationToken);
        SetActivityResult(activity, result);

        return result;
    }

    public async Task<Result<ExtractedRecipeDto>> ExtractFromPhotosAsync(
        IReadOnlyList<PhotoData> photos,
        CancellationToken cancellationToken = default)
    {
        using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("extract.recipe.photos");
        activity?.SetTag("extract.photo_count", photos.Count);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(ExtractionPrompts.PhotoExtraction);

        ChatMessageContentItemCollection messageItems = [new TextContent("Extract the recipe from these images:")];

        foreach (var photo in photos)
        {
            messageItems.Add(new ImageContent(photo.Content, photo.ContentType));
        }

        chatHistory.AddUserMessage(messageItems);

        var result = await CallLlmAndParseAsync(chatHistory, "photo-import", cancellationToken);
        SetActivityResult(activity, result);

        return result;
    }

    public async IAsyncEnumerable<string> StreamExtractAsync(
        ScrapedContent content,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var sanitized = ContentSanitizer.Sanitize(content.CleanedText);

        ChatHistory chatHistory = new();
        chatHistory.AddSystemMessage(ExtractionPrompts.WebExtraction);
        chatHistory.AddUserMessage(ExtractionPrompts.WrapInContentDelimiters(sanitized));

        await foreach (var chunk in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken))
        {
            if (chunk.Content is not null) yield return chunk.Content;
        }
    }

    private static void SetActivityResult(Activity? activity, Result<ExtractedRecipeDto> result)
    {
        activity?.SetTag("extract.success", result.IsSuccess);
        if (result.IsSuccess)
        {
            activity?.SetTag("extract.recipe_title", result.Value!.Title);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.Error?.Message);
        }
    }

    private async Task<Result<ExtractedRecipeDto>> CallLlmAndParseAsync(
        ChatHistory chatHistory,
        string source,
        CancellationToken cancellationToken)
    {
        using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("extract.llm_call");
        activity?.SetTag("llm.source", source);

        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        string response;
        try
        {
            var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            response = result.Content ?? string.Empty;
            activity?.SetTag("llm.response_length", response.Length);
            activity?.SetTag("llm.model", result.ModelId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogLlmCallFailed(source, ex.Message);
            return ImportRecipeErrors.ExtractionFailed;
        }

        return ParseResponse(response, source);
    }

    private Result<ExtractedRecipeDto> ParseResponse(string response, string sourceUrl)
    {
        try
        {
            var json = LlmResponseParser.ExtractJson(response);

            if (json.Contains(ExtractionPrompts.LlmNoRecipeErrorCode, StringComparison.Ordinal))
            {
                LogNoRecipeFound(sourceUrl);
                return ImportRecipeErrors.NoRecipeFound;
            }

            var recipe = JsonSerializer.Deserialize<ExtractedRecipeDto>(json, jsonOptions);
            if (recipe is null)
            {
                LogParsingFailed(sourceUrl, "Deserialization returned null");
                return ImportRecipeErrors.ExtractionFailed;
            }

            return recipe;
        }
        catch (JsonException ex)
        {
            LogParsingFailed(sourceUrl, ex.Message);
            return ImportRecipeErrors.ExtractionFailed;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "LLM call failed for URL {SourceUrl}: {Reason}")]
    private partial void LogLlmCallFailed(string sourceUrl, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No recipe found in content from URL {SourceUrl}")]
    private partial void LogNoRecipeFound(string sourceUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse LLM response for URL {SourceUrl}: {Reason}")]
    private partial void LogParsingFailed(string sourceUrl, string reason);
}
