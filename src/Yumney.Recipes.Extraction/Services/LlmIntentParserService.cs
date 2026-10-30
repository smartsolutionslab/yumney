using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public sealed partial class LlmIntentParserService(IChatClient chatClient, ILogger<LlmIntentParserService> logger)
	: IIntentParserService
{
	public async Task<Result<ParsedIntentDto>> ParseAsync(string userInput, string? pageContext, CancellationToken cancellationToken = default)
	{
		using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("intent.parse");
		activity?.SetTag("intent.input_length", userInput.Length);
		activity?.SetTag("intent.page_context", pageContext ?? "none");

		var userMessage = string.IsNullOrEmpty(pageContext)
			? userInput
			: $"[Page context: {pageContext}] {userInput}";

		List<ChatMessage> messages =
		[
			new(ChatRole.System, IntentParserPrompts.SystemPrompt),
			new(ChatRole.User, userMessage),
		];

		string response;
		try
		{
			var result = await chatClient.GetResponseAsync(messages, LlmJson.OptionsFor<IntentParseResult>(), cancellationToken);
			response = result.Text;
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

	private static Result<ParsedIntentDto> FallbackIntent()
	{
		return new ParsedIntentDto("general_chat", [], null);
	}

	private Result<ParsedIntentDto> ParseResponse(string response)
	{
		try
		{
			var parsed = LlmJson.Deserialize<IntentParseResult>(response);

			if (parsed is null || string.IsNullOrWhiteSpace(parsed.Intent))
			{
				LogParseInvalidResponse(response);
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
			LogParseJsonFailed(ex.Message, response);
			return FallbackIntent();
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Intent parsing LLM call failed: {Reason}")]
	private partial void LogParseFailed(string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Intent parsing returned invalid response: {Response}")]
	private partial void LogParseInvalidResponse(string response);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Intent parsing JSON deserialization failed: {Reason}, response: {Response}")]
	private partial void LogParseJsonFailed(string reason, string response);

	internal sealed record IntentParseResult(
		string? Intent,
		Dictionary<string, string>? Entities,
		string? Clarification);
}
