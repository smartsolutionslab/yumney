using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Extraction.Services;

#pragma warning disable SA1601, SA1311
public sealed partial class SemanticKernelWeekSuggestionService(Kernel kernel, ILogger<SemanticKernelWeekSuggestionService> logger)
	: IWeekSuggestionService
{
	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	private static readonly string[] dayOrder = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

	public async Task<Result<IReadOnlyList<WeekSuggestionEntryDto>>> SuggestAsync(
		WeekIdentifier week,
		IReadOnlyList<RecipeCatalogEntry> catalog,
		IReadOnlyList<MealHistoryEntryDto> recentHistory,
		DietaryProfileSnapshot dietary,
		CancellationToken cancellationToken = default)
	{
		var chatHistory = new ChatHistory();
		chatHistory.AddSystemMessage(WeekSuggestionPrompts.SystemPrompt);
		chatHistory.AddUserMessage(WeekSuggestionPrompts.BuildUserMessage(week, catalog, recentHistory, dietary));

		var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
		var firstResponse = await InvokeAsync(chatCompletion, chatHistory, cancellationToken);
		if (firstResponse is null) return SuggestWeekPlanErrors.SuggestionFailed;

		var parsed = Parse(firstResponse, catalog, quiet: true);
		if (parsed.IsSuccess) return parsed;

		LogParseRetry();
		chatHistory.AddAssistantMessage(firstResponse);
		chatHistory.AddUserMessage(WeekSuggestionPrompts.JsonRepair);

		var secondResponse = await InvokeAsync(chatCompletion, chatHistory, cancellationToken);
		if (secondResponse is null) return SuggestWeekPlanErrors.SuggestionFailed;

		return Parse(secondResponse, catalog, quiet: false);
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

	private Result<IReadOnlyList<WeekSuggestionEntryDto>> Parse(string response, IReadOnlyList<RecipeCatalogEntry> catalog, bool quiet)
	{
		try
		{
			var json = ExtractJson(response);
			var envelope = JsonSerializer.Deserialize<SuggestionEnvelope>(json, jsonOptions);
			if (envelope?.Entries is null || envelope.Entries.Count == 0)
			{
				if (!quiet) LogParsingFailed("Empty or missing entries array");
				return SuggestWeekPlanErrors.SuggestionFailed;
			}

			var titleLookup = catalog.ToDictionary(entry => entry.RecipeIdentifier, entry => entry.Title);
			var orderedEntries = envelope.Entries
				.Where(raw => raw.RecipeIdentifier.HasValue && titleLookup.ContainsKey(raw.RecipeIdentifier.Value))
				.Select(raw => raw.ToDto(titleLookup))
				.OrderBy(entry => DayIndex(entry.Day))
				.ToList();

			if (orderedEntries.Count == 0)
			{
				if (!quiet) LogParsingFailed("No suggested entries referenced a known catalog recipe");
				return SuggestWeekPlanErrors.SuggestionFailed;
			}

			return Result<IReadOnlyList<WeekSuggestionEntryDto>>.Success(orderedEntries);
		}
		catch (JsonException ex)
		{
			if (!quiet) LogParsingFailed(ex.Message);
			return SuggestWeekPlanErrors.SuggestionFailed;
		}
	}

#pragma warning disable SA1204
	private static int DayIndex(string day)
	{
		var index = Array.IndexOf(dayOrder, day);
		return index < 0 ? int.MaxValue : index;
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
#pragma warning restore SA1204

#pragma warning disable SA1402, SA1649
	private sealed record SuggestionEnvelope(IReadOnlyList<SuggestionRow>? Entries);

	private sealed record SuggestionRow(string? Day, Guid? RecipeIdentifier, string? FreshnessLabel, string? Reason)
	{
		public WeekSuggestionEntryDto ToDto(Dictionary<Guid, string> titleLookup) => new(
			this.Day ?? string.Empty,
			"Dinner",
			this.RecipeIdentifier!.Value,
			titleLookup[this.RecipeIdentifier.Value],
			this.FreshnessLabel,
			this.Reason);
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "LLM week-suggestion call failed: {Reason}")]
	private partial void LogLlmCallFailed(string reason);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse LLM week-suggestion response: {Reason}")]
	private partial void LogParsingFailed(string reason);

	[LoggerMessage(Level = LogLevel.Information, Message = "LLM week-suggestion response was not valid JSON; retrying with a repair prompt")]
	private partial void LogParseRetry();
}
