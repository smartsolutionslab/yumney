using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Records that the current user opened a recipe (US-121). Implementations
/// must apply a per-(user, recipe) dedup window so a refresh storm doesn't
/// flood the activity log; only the first view inside the window publishes
/// <c>RecipeViewedIntegrationEvent</c>.
/// </summary>
public interface IRecipeViewTracker
{
	Task TrackAsync(OwnerIdentifier owner, Recipe recipe, CancellationToken cancellationToken = default);
}
