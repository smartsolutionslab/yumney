using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public static class FavoriteStateMappingExtensions
{
	public static FavoriteStateDto ToFavoriteStateDto(this RecipeIdentifier recipe, bool isFavorite) => new(recipe.Value, isFavorite);
}
