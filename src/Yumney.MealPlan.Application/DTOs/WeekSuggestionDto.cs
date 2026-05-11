namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record WeekSuggestionDto(
	string Week,
	IReadOnlyList<WeekSuggestionEntryDto> Entries);
