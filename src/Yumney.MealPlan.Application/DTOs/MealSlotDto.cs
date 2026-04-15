namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record MealSlotDto(
    string Day,
    string MealType,
    string ContentType,
    string State,
    Guid? RecipeIdentifier,
    string? RecipeTitle,
    int Servings,
    string? FreetextLabel,
    string? LeftoverSourceDay,
    string? LeftoverSourceMealType,
    bool IsEmpty);
