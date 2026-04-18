namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ChatResponseDto(
	string Reply,
	IReadOnlyList<ChatRecipeSuggestionDto> Suggestions);
