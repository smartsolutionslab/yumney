namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record SuggestionsResponseDto(
	IReadOnlyList<SuggestionDto> Suggestions,
	IReadOnlyList<string> QuickActions);
