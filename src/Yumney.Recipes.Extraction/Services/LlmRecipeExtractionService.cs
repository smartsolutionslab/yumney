using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public sealed partial class LlmRecipeExtractionService(
	IChatClient chatClient,
	[FromKeyedServices(LlmTasks.Vision)] IChatClient visionClient,
	IExtractionResultCache cache,
	ILogger<LlmRecipeExtractionService> logger)
	: IRecipeExtractionService
{
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

		List<ChatMessage> messages =
		[
			new(ChatRole.System, ExtractionPrompts.WebExtraction),
			new(ChatRole.User, ExtractionPrompts.WrapInContentDelimiters(sanitized)),
		];

		var result = await CallLlmAndParseAsync(new LlmCall(chatClient, messages, sourceUrl), cancellationToken);
		SetActivityResult(activity, result);

		if (result.IsSuccess)
		{
			await cache.SetAsync(cacheKey, result.Value!, cancellationToken);
		}

		return result;
	}

	public async Task<Result<ExtractedRecipeDto>> ExtractFromPhotosAsync(IReadOnlyList<PhotoData> photos, CancellationToken cancellationToken = default)
	{
		using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("extract.recipe.photos");
		activity?.SetTag("extract.photo_count", photos.Count);

		List<AIContent> userContents = [new TextContent("Extract the recipe from these images:")];
		foreach (var photo in photos)
		{
			userContents.Add(new DataContent(photo.Content, photo.ContentType));
		}

		List<ChatMessage> messages =
		[
			new(ChatRole.System, ExtractionPrompts.PhotoExtraction),
			new(ChatRole.User, userContents),
		];

		var result = await CallLlmAndParseAsync(new LlmCall(visionClient, messages, "photo-import"), cancellationToken);
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
			yield return JsonSerializer.Serialize(content.StructuredRecipe, LlmJson.SerializerOptions);
			yield break;
		}

		var sanitized = ContentSanitizer.Sanitize(content.CleanedText);
		List<ChatMessage> messages =
		[
			new(ChatRole.System, ExtractionPrompts.WebExtraction),
			new(ChatRole.User, ExtractionPrompts.WrapInContentDelimiters(sanitized)),
		];

		// Plain JSON mode rather than a schema: the SSE endpoint detects top-level
		// recipe fields while they stream and accepts the error sentinel as-is.
		var options = new ChatOptions { ResponseFormat = ChatResponseFormat.Json };
		await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
		{
			if (update.Text.Length > 0) yield return update.Text;
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

	private async Task<Result<ExtractedRecipeDto>> CallLlmAndParseAsync(LlmCall call, CancellationToken cancellationToken)
	{
		var (_, messages, source) = call;
		using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("extract.llm_call");
		activity?.SetTag("llm.source", source);

		var firstResponse = await InvokeLlmAsync(call, activity, cancellationToken);
		if (firstResponse is null) return ImportRecipeErrors.ExtractionFailed;

		var parsed = ParseResponse(firstResponse, source, quiet: true);
		if (parsed.IsSuccess || IsNoRecipeFound(parsed)) return parsed;

		// Parse failed. Ask the LLM to repair its previous response once.
		LogParseRetry(source);
		messages.Add(new ChatMessage(ChatRole.Assistant, firstResponse));
		messages.Add(new ChatMessage(ChatRole.User, ExtractionPrompts.JsonRepair));

		var secondResponse = await InvokeLlmAsync(call, activity, cancellationToken);
		if (secondResponse is null) return ImportRecipeErrors.ExtractionFailed;

		return ParseResponse(secondResponse, source, quiet: false);
	}

	private async Task<string?> InvokeLlmAsync(LlmCall call, Activity? activity, CancellationToken cancellationToken)
	{
		var (client, messages, source) = call;
		try
		{
			var result = await client.GetResponseAsync(messages, LlmJson.OptionsFor<RecipeExtractionResponse>(), cancellationToken);
			var response = result.Text;
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
			var parsed = LlmJson.Deserialize<RecipeExtractionResponse>(response);
			if (parsed?.IsNoRecipeFound == true)
			{
				LogNoRecipeFound(sourceUrl);
				return ImportRecipeErrors.NoRecipeFound;
			}

			var recipe = parsed?.ToDto();
			if (recipe is null)
			{
				if (!quiet) LogParsingFailed(sourceUrl, "Response had no recipe title");
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

	// URL extraction runs on the default model, photo import on the vision model.
	private sealed record LlmCall(IChatClient Client, List<ChatMessage> Messages, string Source);
}
