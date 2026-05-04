using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.Recipes.Application.Common;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601, SA1311
public sealed partial class SemanticKernelRecipeSuggestionService(
	Kernel kernel,
	ILogger<SemanticKernelRecipeSuggestionService> logger)
	: IRecipeSuggestionService
{
	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public async Task<Result<IReadOnlyList<ExtractedRecipeDto>>> SuggestAsync(
		IReadOnlyCollection<string> availableIngredients,
		string? dietaryType,
		IReadOnlyCollection<string> restrictions,
		int count,
		CancellationToken cancellationToken = default)
	{
		var chatHistory = new ChatHistory();
		chatHistory.AddSystemMessage(SuggestionPrompts.SystemPrompt);
		chatHistory.AddUserMessage(SuggestionPrompts.BuildUserMessage(availableIngredients, dietaryType, restrictions, count));

		var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
		var firstResponse = await InvokeAsync(chatCompletion, chatHistory, cancellationToken);
		if (firstResponse is null) return RecipeSuggestionErrors.SuggestionFailed;

		var parsed = Parse(firstResponse, quiet: true);
		if (parsed.IsSuccess) return parsed;

		LogParseRetry();
		chatHistory.AddAssistantMessage(firstResponse);
		chatHistory.AddUserMessage(SuggestionPrompts.JsonRepair);

		var secondResponse = await InvokeAsync(chatCompletion, chatHistory, cancellationToken);
		if (secondResponse is null) return RecipeSuggestionErrors.SuggestionFailed;

		return Parse(secondResponse, quiet: false);
	}

	private async Task<string?> InvokeAsync(IChatCompletionService chat, ChatHistory history, CancellationToken cancellationToken)
	{
		try
		{
			var result = await chat.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
			return result.Content ?? string.Empty;
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
			var json = LlmResponseParser.ExtractJson(response);
			var envelope = JsonSerializer.Deserialize<SuggestionEnvelope>(json, jsonOptions);
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
	private sealed record SuggestionEnvelope(IReadOnlyList<ExtractedRecipeDto>? Recipes);

	[LoggerMessage(Level = LogLevel.Warning, Message = "LLM suggestion call failed: {Reason}")]
	private partial void LogLlmCallFailed(string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse LLM suggestion response: {Reason}")]
	private partial void LogParsingFailed(string reason);

	[LoggerMessage(Level = LogLevel.Information, Message = "LLM suggestion response was not valid JSON; retrying with a repair prompt")]
	private partial void LogParseRetry();
}
