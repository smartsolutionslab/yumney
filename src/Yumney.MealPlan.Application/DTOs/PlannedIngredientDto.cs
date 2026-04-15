namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record PlannedIngredientDto(
    string ItemName,
    decimal Quantity,
    string? Unit,
    string Source,
    int Servings);
