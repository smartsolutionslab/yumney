using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Common;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
#pragma warning disable SA1311
public sealed partial class SemanticKernelRecipeExtractionService(
	Kernel kernel,
	IExtractionResultCache cache,
	ILogger<SemanticKernelRecipeExtractionService> logger)
	: IRecipeExtractionService
{
	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public async Task<Result<ExtractedRecipeDto>> ExtractAsync(ScrapedContent content, CancellationToken cancellationToken = default)
	{
		var cleanedText = content.CleanedText;
		var sourceUrl = content.SourceUrl?.Value ?? string.Empty;

		using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("extract.recipe.url");
		activity?.SetTag("extract.source", sourceUrl);

		if (content.StructuredRecipe is not null)
		{
			activity?.SetTag("extract.strategy", "json-ld");
			activity?.SetStatus(ActivityStatusCode.Ok);
			return content.StructuredRecipe;
		}

		activity?.SetTag("extract.strategy", "llm");
		activity?.SetTag("extract.content_length", cleanedText.Length);

		var sanitized = ContentSanitizer.Sanitize(cleanedText);

		var cacheKey = cache.ComputeKey(sanitized);
		var cached = await cache.GetAsync(cacheKey, cancellationToken);
		if (cached is not null)
		{
			activity?.SetTag("extract.cache", "hit");
			activity?.SetStatus(ActivityStatusCode.Ok);
			LogCacheHit(sourceUrl);
			return cached;
		}

		activity?.SetTag("extract.cache", "miss");

		var chatHistory = new ChatHistory();
		chatHistory.AddSystemMessage(ExtractionPrompts.WebExtraction);
		chatHistory.AddUserMessage(ExtractionPrompts.WrapInContentDelimiters(sanitized));

		var result = await CallLlmAndParseAsync(chatHistory, sourceUrl, cancellationToken);
		SetActivityResult(activity, result);

		if (result.IsSuccess)
		{
			await cache.SetAsync(cacheKey, result.Value!, cancellationToken);
		}

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
		if (content.StructuredRecipe is not null)
		{
			// JSON-LD path: emit the final JSON in one chunk; the SSE endpoint
			// treats the accumulated buffer as the completed payload.
			yield return JsonSerializer.Serialize(content.StructuredRecipe, jsonOptions);
			yield break;
		}

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

		var firstResponse = await InvokeLlmAsync(chatCompletion, chatHistory, source, activity, cancellationToken);
		if (firstResponse is null) return ImportRecipeErrors.ExtractionFailed;

		var parsed = ParseResponse(firstResponse, source, quiet: true);
		if (parsed.IsSuccess || IsNoRecipeFound(parsed)) return parsed;

		// Parse failed. Ask the LLM to repair its previous response once.
		LogParseRetry(source);
		chatHistory.AddAssistantMessage(firstResponse);
		chatHistory.AddUserMessage(ExtractionPrompts.JsonRepair);

		var secondResponse = await InvokeLlmAsync(chatCompletion, chatHistory, source, activity, cancellationToken);
		if (secondResponse is null) return ImportRecipeErrors.ExtractionFailed;

		return ParseResponse(secondResponse, source, quiet: false);
	}

	private async Task<string?> InvokeLlmAsync(
		IChatCompletionService chatCompletion,
		ChatHistory chatHistory,
		string source,
		Activity? activity,
		CancellationToken cancellationToken)
	{
		try
		{
			var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
			var response = result.Content ?? string.Empty;
			activity?.SetTag("llm.response_length", response.Length);
			activity?.SetTag("llm.model", result.ModelId);
			return response;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			LogLlmCallFailed(source, ex.Message);
			return null;
		}
	}

#pragma warning disable SA1204
	private static bool IsNoRecipeFound(Result<ExtractedRecipeDto> result)
		=> result.IsFailure && result.Error == ImportRecipeErrors.NoRecipeFound;
#pragma warning restore SA1204

	private Result<ExtractedRecipeDto> ParseResponse(string response, string sourceUrl, bool quiet = false)
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
				if (!quiet) LogParsingFailed(sourceUrl, "Deserialization returned null");
				return ImportRecipeErrors.ExtractionFailed;
			}

			return recipe;
		}
		catch (JsonException ex)
		{
			if (!quiet) LogParsingFailed(sourceUrl, ex.Message);
			return ImportRecipeErrors.ExtractionFailed;
		}
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "LLM call failed for URL {SourceUrl}: {Reason}")]
	private partial void LogLlmCallFailed(string sourceUrl, string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "No recipe found in content from URL {SourceUrl}")]
	private partial void LogNoRecipeFound(string sourceUrl);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse LLM response for URL {SourceUrl}: {Reason}")]
	private partial void LogParsingFailed(string sourceUrl, string reason);

	[LoggerMessage(Level = LogLevel.Information, Message = "LLM response for {SourceUrl} was not valid JSON; retrying with a repair prompt")]
	private partial void LogParseRetry(string sourceUrl);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Extraction cache hit for {SourceUrl}; skipping LLM")]
	private partial void LogCacheHit(string sourceUrl);
}
