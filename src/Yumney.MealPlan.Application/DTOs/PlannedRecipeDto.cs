namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

/// <summary>
/// A recipe planned for the week with its serving count.
/// Used to generate shopping lists — only Recipe slots contribute, not Leftover/Freetext.
/// </summary>
public sealed record PlannedRecipeDto(
    Guid RecipeIdentifier,
    string RecipeTitle,
    int Servings,
    string Day,
    string MealType);
