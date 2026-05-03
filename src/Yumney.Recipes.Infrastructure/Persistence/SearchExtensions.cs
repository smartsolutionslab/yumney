using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public static class SearchExtensions
{
	public static IQueryable<Recipe> ApplySearch(this IQueryable<Recipe> query, SearchTerm? search)
	{
		if (search is null) return query;

		var pattern = $"%{search.Value}%";

		query = query.Where(recipe =>
			EF.Functions.ILike(recipe.Title, pattern) ||
			(recipe.Description != null && EF.Functions.ILike(recipe.Description, pattern)) ||
			recipe.Ingredients.Any(ingredient => EF.Functions.ILike(ingredient.Name, pattern)));

		return query;
	}
}
