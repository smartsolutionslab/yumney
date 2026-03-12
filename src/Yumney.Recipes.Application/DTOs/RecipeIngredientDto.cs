namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record RecipeIngredientDto(string Name, decimal? Amount, string? Unit);
