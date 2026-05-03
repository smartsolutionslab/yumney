using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public static class FilterExtensions
{
	extension(IQueryable<Recipe> query)
	{
		public IQueryable<Recipe> ApplyTags(IReadOnlyList<RecipeTag>? tags)
		{
			if (tags is null || tags.Count <= 0) return query;

			// AND logic: every requested tag must be present on the recipe.
			foreach (var requiredTag in tags)
			{
				var tagValue = requiredTag.Value;
				query = query.Where(recipe => recipe.Tags.Any(tag => tag.Value == tagValue));
			}

			return query;
		}

		public IQueryable<Recipe> ApplyCookingTime(CookingTime? maxCookTime)
		{
			if (maxCookTime is null) return query;

			query = query.Where(recipe => recipe.Timing != null && recipe.Timing.Cooking != null && recipe.Timing.Cooking <= maxCookTime);

			return query;
		}

		public IQueryable<Recipe> ApplyMaxPrepTime(PreparationTime? maxPrepTime)
		{
			if (maxPrepTime is null) return query;

			query = query.Where(recipe => recipe.Timing != null && recipe.Timing.Preparation != null && recipe.Timing.Preparation <= maxPrepTime);

			return query;
		}

		public IQueryable<Recipe> ApplyDifficulty(Difficulty? difficulty)
		{
			if (difficulty is null) return query;

			query = query.Where(recipe => recipe.Difficulty == difficulty);

			return query;
		}

		public IQueryable<Recipe> ApplyFavoritesOnly(
			bool? favoritesOnly,
			IQueryable<RecipeIdentifier> favoritesRecipesOfUserQuery)
		{
			if (favoritesOnly != true) return query;

			query = query.Where(recipe => favoritesRecipesOfUserQuery.Contains(recipe.Id));

			return query;
		}

		public IQueryable<Recipe> ApplyFilter(
			RecipeFilter? filter,
			IQueryable<RecipeIdentifier> favoriteRecipesOfUserQuery)
		{
			if (filter is null || filter.IsEmpty) return query;

			var (tags, difficulty, maxPrepTime, maxCookTime, favoritesOnly) = filter;

			query = query
				.ApplyDifficulty(difficulty)
				.ApplyMaxPrepTime(maxPrepTime)
				.ApplyCookingTime(maxCookTime)
				.ApplyTags(tags)
				.ApplyFavoritesOnly(favoritesOnly, favoriteRecipesOfUserQuery);

			return query;
		}
	}
}
