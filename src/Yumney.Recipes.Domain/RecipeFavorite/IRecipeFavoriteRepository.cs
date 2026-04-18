using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

public interface IRecipeFavoriteRepository
{
	Task<bool> IsFavoritedAsync(
		OwnerIdentifier owner,
		RecipeIdentifier recipeIdentifier,
		CancellationToken cancellationToken = default);

	Task<IReadOnlySet<Guid>> GetFavoritedIdsAsync(
		OwnerIdentifier owner,
		IReadOnlyCollection<RecipeIdentifier> recipeIdentifiers,
		CancellationToken cancellationToken = default);

	Task AddAsync(RecipeFavorite favorite, CancellationToken cancellationToken = default);

	Task RemoveAsync(
		OwnerIdentifier owner,
		RecipeIdentifier recipeIdentifier,
		CancellationToken cancellationToken = default);
}
