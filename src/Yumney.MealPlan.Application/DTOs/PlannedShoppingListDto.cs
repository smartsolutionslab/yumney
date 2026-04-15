namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

/// <summary>
/// All ingredients needed for a weekly meal plan.
/// </summary>
public sealed record PlannedShoppingListDto(
    string Week,
    IReadOnlyList<PlannedIngredientDto> Ingredients);
