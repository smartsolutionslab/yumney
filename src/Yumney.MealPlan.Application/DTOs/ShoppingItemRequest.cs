namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record ShoppingItemRequest(string ItemName, decimal Quantity, string? Unit, string Source);
