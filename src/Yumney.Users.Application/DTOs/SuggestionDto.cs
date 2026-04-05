namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record SuggestionDto(
    Guid RecipeIdentifier,
    string Title,
    string? ImageUrl,
    int? PrepTimeMinutes,
    string Reason);

public sealed record SuggestionsResponseDto(
    IReadOnlyList<SuggestionDto> Suggestions,
    IReadOnlyList<string> QuickActions);
