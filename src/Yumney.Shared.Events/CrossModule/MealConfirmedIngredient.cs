namespace SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

public sealed record MealConfirmedIngredient(string Name, decimal Quantity, string? Unit);
