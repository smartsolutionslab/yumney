namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record CookedIngredient(string Name, decimal Quantity, string? Unit);
