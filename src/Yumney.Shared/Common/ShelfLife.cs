namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Coarse shelf-life lookup keyed by <see cref="IngredientCategory"/> (US-341).
/// The values are deliberately conservative — the goal is a "use soon" nudge,
/// not a precise expiry date. Returning <c>null</c> means the category is not
/// tracked for freshness (pantry, household, beverages, other).
/// </summary>
public static class ShelfLife
{
	public static int? DaysFor(IngredientCategory category)
	{
		var key = category?.Value ?? IngredientCategory.Other.Value;

		return key switch
		{
			"meat-fish" => 2,
			"bakery" => 3,
			"produce" => 5,
			"dairy" => 6,
			"frozen" => 60,
			_ => null,
		};
	}

	/// <summary>
	/// Returns the freshness state for an item bought <paramref name="daysSinceBought"/>
	/// days ago. <c>null</c> <paramref name="daysSinceBought"/> (no purchase recorded)
	/// always yields <see cref="Freshness.NotTracked"/>.
	/// </summary>
	/// <param name="category">Ingredient category — drives the shelf-life lookup.</param>
	/// <param name="daysSinceBought">Days since the most recent purchase, or null if unknown.</param>
	/// <returns>The corresponding <see cref="Freshness"/> classification.</returns>
	public static Freshness Classify(IngredientCategory category, int? daysSinceBought)
	{
		var shelf = DaysFor(category);
		if (shelf is null || daysSinceBought is null) return Freshness.NotTracked;
		if (daysSinceBought.Value >= shelf.Value) return Freshness.CheckIt;
		if (daysSinceBought.Value * 2 >= shelf.Value) return Freshness.UseSoon;
		return Freshness.Fresh;
	}
}
