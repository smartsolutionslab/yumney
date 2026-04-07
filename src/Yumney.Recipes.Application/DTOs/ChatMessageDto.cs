namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ChatMessageDto(string Role, string Content);

public sealed record ChatRequestDto(string Message, IReadOnlyList<ChatMessageDto> History);

public sealed record ChatResponseDto(
    string Reply,
    IReadOnlyList<ChatRecipeSuggestionDto> Suggestions);

public sealed record ChatRecipeSuggestionDto(
    Guid? RecipeIdentifier,
    string Title,
    string? Reason);
