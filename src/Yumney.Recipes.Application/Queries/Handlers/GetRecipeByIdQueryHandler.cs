using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

public sealed class GetRecipeByIdQueryHandler(
	IRecipeRepository recipes,
	IRecipeFavoriteRepository favorites,
	ICurrentUser currentUser)
	: IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>>
{
	public async Task<Result<RecipeDetailDto>> HandleAsync(GetRecipeByIdQuery query, CancellationToken cancellationToken = default)
	{
		var identifier = query.Identifier;
		var owner = currentUser.AsOwner();

		var recipe = await recipes.GetByIdAsync(identifier, cancellationToken);

		if (recipe.Owner != owner)
		{
			return GetRecipeByIdErrors.AccessDenied;
		}

		var isFavorite = await favorites.IsFavoritedAsync(owner, identifier, cancellationToken);
		return recipe.ToDetailDto(isFavorite);
	}
}
