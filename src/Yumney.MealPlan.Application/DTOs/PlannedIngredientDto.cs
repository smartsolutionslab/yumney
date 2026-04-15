namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

/// <summary>
/// An ingredient needed from the meal plan — ready to be added to the shopping list.
/// </summary>
public sealed record PlannedIngredientDto(
    string ItemName,
    decimal Quantity,
    string? Unit,
    string Source,
    int Servings);
