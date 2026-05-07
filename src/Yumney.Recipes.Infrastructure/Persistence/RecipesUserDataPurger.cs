using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

/// <summary>
/// EF-backed implementation of <see cref="IRecipesUserDataPurger"/>. Bulk-deletes
/// every Recipe and RecipeFavorite scoped to the given Keycloak user id.
/// </summary>
public sealed class RecipesUserDataPurger(RecipesDbContext context) : IRecipesUserDataPurger
{
	public async Task PurgeAsync(string keycloakUserId, CancellationToken cancellationToken = default)
	{
		var owner = OwnerIdentifier.From(keycloakUserId);

		await context.RecipeFavorites
			.Where(favorite => favorite.Owner == owner)
			.ExecuteDeleteAsync(cancellationToken);

		await context.Recipes
			.Where(recipe => recipe.Owner == owner)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
