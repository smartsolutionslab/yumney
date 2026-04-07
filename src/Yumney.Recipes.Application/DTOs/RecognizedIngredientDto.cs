namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record RecognizedIngredientDto(string Name, double Confidence, string? Category);

public sealed record RecognizedIngredientsResponseDto(IReadOnlyList<RecognizedIngredientDto> Ingredients);
