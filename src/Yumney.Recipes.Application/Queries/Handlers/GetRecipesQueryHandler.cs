using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

public sealed class GetRecipesQueryHandler(
	IRecipeRepository recipes,
	IRecipeFavoriteRepository favorites,
	ICurrentUser currentUser)
	: IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>
{
	public async Task<Result<PagedResult<RecipeListItemDto>>> HandleAsync(GetRecipesQuery query, CancellationToken cancellationToken = default)
	{
		var (paging, sorting, search, filter) = query;
		var owner = currentUser.AsOwner();

		var (items, totalCount) = await recipes.GetByOwnerAsync(owner, paging, sorting, search, filter, cancellationToken);

		var favoritedIds = await favorites.GetFavoritedIdsAsync(
			owner,
			items.Select(recipe => recipe.Id).ToList(),
			cancellationToken);

		var dtoItems = items
			.Select(recipe => recipe.ToListItemDto(favoritedIds.Contains(recipe.Id.Value)))
			.ToList();

		return PagedResultExtensions.With(dtoItems, totalCount, paging);
	}
}
