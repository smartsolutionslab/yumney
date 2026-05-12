using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

public interface IRecipeFavoriteRepository
{
	Task<bool> IsFavoritedAsync(OwnerIdentifier owner, RecipeIdentifier recipe, CancellationToken cancellationToken = default);

	Task<IReadOnlySet<Guid>> GetFavoritedIdsAsync(
		OwnerIdentifier owner,
		IReadOnlyCollection<RecipeIdentifier> recipes,
		CancellationToken cancellationToken = default);

	Task AddAsync(RecipeFavorite favorite, CancellationToken cancellationToken = default);

	Task RemoveAsync(OwnerIdentifier owner, RecipeIdentifier recipe, CancellationToken cancellationToken = default);
}
