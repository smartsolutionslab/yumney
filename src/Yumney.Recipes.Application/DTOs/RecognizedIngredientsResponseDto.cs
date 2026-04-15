namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record RecognizedIngredientsResponseDto(IReadOnlyList<RecognizedIngredientDto> Ingredients);
