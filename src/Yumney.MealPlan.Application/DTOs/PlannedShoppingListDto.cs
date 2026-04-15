namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record PlannedShoppingListDto(
    string Week,
    IReadOnlyList<PlannedIngredientDto> Ingredients);
