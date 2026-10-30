using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Extraction.Services;

#pragma warning disable SA1601, SA1311
public sealed partial class LlmWeekSuggestionService(IChatClient chatClient, ILogger<LlmWeekSuggestionService> logger)
	: IWeekSuggestionService
{
	private static readonly string[] dayOrder = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

	public async Task<Result<IReadOnlyList<WeekSuggestionEntryDto>>> SuggestAsync(
		WeekIdentifier week,
		IReadOnlyList<RecipeCatalogEntry> catalog,
		IReadOnlyList<MealHistoryEntryDto> recentHistory,
		DietaryProfileSnapshot dietary,
		CancellationToken cancellationToken = default)
	{
		List<ChatMessage> messages =
		[
			new(ChatRole.System, WeekSuggestionPrompts.SystemPrompt),
			new(ChatRole.User, WeekSuggestionPrompts.BuildUserMessage(week, catalog, recentHistory, dietary)),
		];

		var firstResponse = await InvokeAsync(messages, cancellationToken);
		if (firstResponse is null) return SuggestWeekPlanErrors.SuggestionFailed;

		var parsed = Parse(firstResponse, catalog, quiet: true);
		if (parsed.IsSuccess) return parsed;

		LogParseRetry();
		messages.Add(new ChatMessage(ChatRole.Assistant, firstResponse));
		messages.Add(new ChatMessage(ChatRole.User, WeekSuggestionPrompts.JsonRepair));

		var secondResponse = await InvokeAsync(messages, cancellationToken);
		if (secondResponse is null) return SuggestWeekPlanErrors.SuggestionFailed;

		return Parse(secondResponse, catalog, quiet: false);
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

	private Result<IReadOnlyList<WeekSuggestionEntryDto>> Parse(string response, IReadOnlyList<RecipeCatalogEntry> catalog, bool quiet)
	{
		try
		{
			var envelope = LlmJson.Deserialize<SuggestionEnvelope>(response);
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
#pragma warning restore SA1204

#pragma warning disable SA1402, SA1649
	internal sealed record SuggestionEnvelope(IReadOnlyList<SuggestionRow>? Entries);

	internal sealed record SuggestionRow(string? Day, Guid? RecipeIdentifier, string? FreshnessLabel, string? Reason)
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
