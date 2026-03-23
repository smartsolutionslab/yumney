namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

public sealed record SaveRecipeIngredientRequest(string Name, decimal? Amount, string? Unit);
