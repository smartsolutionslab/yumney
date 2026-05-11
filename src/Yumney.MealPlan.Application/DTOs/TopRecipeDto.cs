namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record TopRecipeDto(Guid RecipeIdentifier, string RecipeTitle, int CookCount);
