using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

public sealed class GetRecipesQueryHandler(IRecipeRepository recipes, IRecipeFavoriteRepository favorites, ICurrentUser currentUser)
	: IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>
{
	public async Task<Result<PagedResult<RecipeListItemDto>>> HandleAsync(GetRecipesQuery query, CancellationToken cancellationToken = default)
	{
		var (paging, sorting, search, filter) = query;
		var owner = currentUser.AsOwner();

		var page = await recipes.GetByOwnerAsync(owner, paging, sorting, search, filter, cancellationToken);
		var idsOfRecpies = page.Items.Select(recipe => recipe.Id).ToList();

		var favoritedIds = await favorites.GetFavoritedIdsAsync(owner, idsOfRecpies, cancellationToken);

		return page.Map(recipe => recipe.ToListItemDto(favoritedIds.Contains(recipe.Id.Value)));
	}
}
