namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record FavoriteStateDto(Guid RecipeIdentifier, bool IsFavorite);
