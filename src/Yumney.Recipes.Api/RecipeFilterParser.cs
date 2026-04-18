using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

internal static class RecipeFilterParser
{
	public static RecipeFilter? Build(
		string? tags,
		string? difficulty,
		int? maxPrepTime,
		int? maxCookTime,
		bool? favoritesOnly = null)
	{
		var tagList = ParseTags(tags);
		var difficultyVo = Difficulty.FromNullable(difficulty);
		var maxPrep = PreparationTime.FromNullable(maxPrepTime);
		var maxCook = CookingTime.FromNullable(maxCookTime);

		if (tagList is null && difficultyVo is null && maxPrep is null && maxCook is null && favoritesOnly is not true)
		{
			return null;
		}

		return new RecipeFilter(tagList, difficultyVo, maxPrep, maxCook, favoritesOnly);

		static List<RecipeTag>? ParseTags(string? tags)
		{
			if (string.IsNullOrWhiteSpace(tags)) return null;

			var parsed = tags
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(RecipeTag.From)
				.ToList();

			return parsed.Count == 0 ? null : parsed;
		}
	}
}
