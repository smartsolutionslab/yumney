using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public sealed partial class SemanticKernelIntentParserService(
    Kernel kernel,
    ILogger<SemanticKernelIntentParserService> logger) : IIntentParserService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<ParsedIntentDto>> ParseAsync(
        string userInput,
        string? pageContext,
        CancellationToken cancellationToken = default)
    {
        using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("intent.parse");
        activity?.SetTag("intent.input_length", userInput.Length);
        activity?.SetTag("intent.page_context", pageContext ?? "none");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(IntentParserPrompts.SystemPrompt);

        var userMessage = string.IsNullOrEmpty(pageContext)
            ? userInput
            : $"[Page context: {pageContext}] {userInput}";

        chatHistory.AddUserMessage(userMessage);

        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        string response;
        try
        {
            var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            response = result.Content ?? string.Empty;
            activity?.SetTag("intent.response_length", response.Length);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogParseFailed(ex.Message);
            return new ApiError("intent.parse.failed", "Failed to parse intent. Please try again.", 502);
        }

        return ParseResponse(response);
    }

    private static string ExtractJson(string response)
    {
        var trimmed = response.Trim();

        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            var endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (endIndex > 7)
            {
                trimmed = trimmed[7..endIndex].Trim();
            }
        }
        else if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (endIndex > 3)
            {
                trimmed = trimmed[3..endIndex].Trim();
            }
        }

        return trimmed;
    }

    private static Result<ParsedIntentDto> FallbackIntent()
    {
        return new ParsedIntentDto("general_chat", [], null);
    }

    private Result<ParsedIntentDto> ParseResponse(string response)
    {
        var json = ExtractJson(response);

        try
        {
            var parsed = JsonSerializer.Deserialize<IntentParseResult>(json, JsonOptions);

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Intent))
            {
                LogParseInvalidResponse(json);
                return FallbackIntent();
            }

            var entities = parsed.Entities ?? [];

            return new ParsedIntentDto(
                parsed.Intent.ToLowerInvariant().Trim(),
                new Dictionary<string, string>(entities, StringComparer.OrdinalIgnoreCase),
                parsed.Clarification);
        }
        catch (JsonException ex)
        {
            LogParseJsonFailed(ex.Message, json);
            return FallbackIntent();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Intent parsing LLM call failed: {Reason}")]
    private partial void LogParseFailed(string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Intent parsing returned invalid response: {Response}")]
    private partial void LogParseInvalidResponse(string response);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Intent parsing JSON deserialization failed: {Reason}, response: {Response}")]
    private partial void LogParseJsonFailed(string reason, string response);

    private sealed record IntentParseResult(
        string? Intent,
        Dictionary<string, string>? Entities,
        string? Clarification);
}
