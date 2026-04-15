namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ChatRecipeSuggestionDto(
    Guid? RecipeIdentifier,
    string Title,
    string? Reason);
