namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed record SaveRecipeIngredient(string Name, decimal? Amount, string? Unit);
