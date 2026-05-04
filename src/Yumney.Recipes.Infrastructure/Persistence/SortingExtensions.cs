using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public static class SortingExtensions
{
	public static IQueryable<Recipe> ApplySorting(this IQueryable<Recipe> query, SortingOptions<RecipeSortField> sorting)
	{
		return (sorting.SortBy, sorting.Direction) switch
		{
			(RecipeSortField.Name, SortDirection.Ascending) => query.OrderBy(recipe => recipe.Title),
			(RecipeSortField.Name, SortDirection.Descending) => query.OrderByDescending(recipe => recipe.Title),
			(RecipeSortField.Date, SortDirection.Ascending) => query.OrderBy(recipe => recipe.CreatedAt),
			(RecipeSortField.Date, SortDirection.Descending) => query.OrderByDescending(recipe => recipe.CreatedAt),

			// Unrated recipes are pushed to the bottom for both directions —
			// "show me my best" should never start with a wall of nulls.
			(RecipeSortField.Rating, SortDirection.Ascending) =>
				query.OrderBy(recipe => recipe.Rating == null).ThenBy(recipe => recipe.Rating),
			(RecipeSortField.Rating, SortDirection.Descending) =>
				query.OrderBy(recipe => recipe.Rating == null).ThenByDescending(recipe => recipe.Rating),

			_ => throw new InvalidOperationException($"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
		};
	}
}
