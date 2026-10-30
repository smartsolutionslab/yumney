using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public sealed partial class LlmRecipeSuggestionService(IChatClient chatClient, ILogger<LlmRecipeSuggestionService> logger)
	: IRecipeSuggestionService
{
	public async Task<Result<IReadOnlyList<ExtractedRecipeDto>>> SuggestAsync(
		IReadOnlyCollection<string> availableIngredients,
		string? dietaryType,
		IReadOnlyCollection<string> restrictions,
		int count,
		CancellationToken cancellationToken = default)
	{
		List<ChatMessage> messages =
		[
			new(ChatRole.System, SuggestionPrompts.SystemPrompt),
			new(ChatRole.User, SuggestionPrompts.BuildUserMessage(availableIngredients, dietaryType, restrictions, count)),
		];

		var firstResponse = await InvokeAsync(messages, cancellationToken);
		if (firstResponse is null) return RecipeSuggestionErrors.SuggestionFailed;

		var parsed = Parse(firstResponse, quiet: true);
		if (parsed.IsSuccess) return parsed;

		LogParseRetry();
		messages.Add(new ChatMessage(ChatRole.Assistant, firstResponse));
		messages.Add(new ChatMessage(ChatRole.User, SuggestionPrompts.JsonRepair));

		var secondResponse = await InvokeAsync(messages, cancellationToken);
		if (secondResponse is null) return RecipeSuggestionErrors.SuggestionFailed;

		return Parse(secondResponse, quiet: false);
	}

	private async Task<string?> InvokeAsync(List<ChatMessage> messages, CancellationToken cancellationToken)
	{
		try
		{
			var result = await chatClient.GetResponseAsync(messages, LlmJson.OptionsFor<SuggestionEnvelope>(), cancellationToken);
			return result.Text;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			LogLlmCallFailed(ex.Message);
			return null;
		}
	}

	private Result<IReadOnlyList<ExtractedRecipeDto>> Parse(string response, bool quiet)
	{
		try
		{
			var envelope = LlmJson.Deserialize<SuggestionEnvelope>(response);
			if (envelope?.Recipes is null || envelope.Recipes.Count == 0)
			{
				if (!quiet) LogParsingFailed("Empty or missing recipes array");
				return RecipeSuggestionErrors.SuggestionFailed;
			}

			List<ExtractedRecipeDto> usable = [.. envelope.Recipes.Where(IsUsable)];
			if (usable.Count == 0)
			{
				if (!quiet) LogParsingFailed("All suggested recipes were missing required fields");
				return RecipeSuggestionErrors.SuggestionFailed;
			}

			return Result<IReadOnlyList<ExtractedRecipeDto>>.Success(usable);
		}
		catch (JsonException ex)
		{
			if (!quiet) LogParsingFailed(ex.Message);
			return RecipeSuggestionErrors.SuggestionFailed;
		}
	}

#pragma warning disable SA1204
	private static bool IsUsable(ExtractedRecipeDto recipe) =>
		!string.IsNullOrWhiteSpace(recipe.Title)
		&& recipe.Ingredients is { Count: > 0 }
		&& recipe.Steps is { Count: > 0 };
#pragma warning restore SA1204

#pragma warning disable SA1402, SA1649
	internal sealed record SuggestionEnvelope(IReadOnlyList<ExtractedRecipeDto>? Recipes);

	[LoggerMessage(Level = LogLevel.Warning, Message = "LLM suggestion call failed: {Reason}")]
	private partial void LogLlmCallFailed(string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse LLM suggestion response: {Reason}")]
	private partial void LogParsingFailed(string reason);

	[LoggerMessage(Level = LogLevel.Information, Message = "LLM suggestion response was not valid JSON; retrying with a repair prompt")]
	private partial void LogParseRetry();
}
