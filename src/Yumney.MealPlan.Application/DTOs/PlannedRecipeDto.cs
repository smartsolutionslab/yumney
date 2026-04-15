namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record PlannedRecipeDto(
    Guid RecipeIdentifier,
    string RecipeTitle,
    int Servings,
    string Day,
    string MealType);
