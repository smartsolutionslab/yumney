namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record SaveRecipeIngredientRequest(string Name, decimal? Amount, string? Unit);
