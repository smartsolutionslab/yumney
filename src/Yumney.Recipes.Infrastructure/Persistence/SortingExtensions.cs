using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

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
			_ => throw new InvalidOperationException($"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
		};
	}
}
