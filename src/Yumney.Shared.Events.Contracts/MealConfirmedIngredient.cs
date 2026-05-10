namespace SmartSolutionsLab.Yumney.Shared.Events.Contracts;

public sealed record MealConfirmedIngredient(string Name, decimal Quantity, string? Unit);
