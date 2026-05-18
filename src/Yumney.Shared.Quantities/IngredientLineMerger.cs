namespace SmartSolutionsLab.Yumney.Shared.Quantities;

/// <summary>
/// Pure logic for "scale each recipe's ingredients to the desired servings
/// then merge identical lines across recipes". Lives in Shared.Quantities so
/// both Shopping and MealPlan can apply the same merge semantics without
/// copying the math (and the same subtle rules — case-insensitive name
/// match, unit-sensitive grouping, null-amount preservation, smart-rounding
/// of integer originals).
///
/// Inputs and outputs are primitives: callers wrap/unwrap their own domain
/// types at the boundary so no module's domain leaks here.
/// </summary>
public static class IngredientLineMerger
{
	public static IReadOnlyList<MergedIngredientLine> Merge(IEnumerable<IngredientLineMergeInput> inputs)
	{
		List<ScaledIngredientLine> scaled = [];
		foreach (var input in inputs)
		{
			foreach (var ingredient in input.Ingredients)
			{
				scaled.Add(new ScaledIngredientLine(
					ingredient.Name,
					ScaleAmount(ingredient.Amount, ingredient.RecipeServings, input.DesiredServings),
					ingredient.Unit));
			}
		}

		return scaled
			.GroupBy(line => new MergeKey(line.Name.Trim(), line.Unit?.Trim()))
			.Select(BuildMerged)
			.ToList();
	}

	private static decimal? ScaleAmount(decimal? amount, int? recipeServings, int? desiredServings)
	{
		if (amount is null) return null;
		if (desiredServings is null || recipeServings is null or <= 0) return amount;
		if (recipeServings == desiredServings.Value) return amount;

		var ratio = (decimal)desiredServings.Value / recipeServings.Value;
		var scaled = amount.Value * ratio;
		return amount.Value % 1 == 0
			? Math.Round(scaled)
			: Math.Round(scaled, 2);
	}

	private static MergedIngredientLine BuildMerged(IGrouping<MergeKey, ScaledIngredientLine> group)
	{
		var first = group.First();
		var allNull = group.All(line => line.Amount is null);
		if (allNull)
		{
			return new MergedIngredientLine(first.Name, Amount: null, first.Unit);
		}

		var summed = group.Where(line => line.Amount.HasValue).Sum(line => line.Amount!.Value);
		return new MergedIngredientLine(first.Name, summed, first.Unit);
	}

	private sealed record ScaledIngredientLine(string Name, decimal? Amount, string? Unit);

	private sealed record MergeKey(string Name, string? Unit)
	{
		public bool Equals(MergeKey? other) =>
			other is not null
			&& string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(Unit, other.Unit, StringComparison.OrdinalIgnoreCase);

		public override int GetHashCode() => HashCode.Combine(
			Name.ToLowerInvariant(),
			Unit?.ToLowerInvariant());
	}
}

/// <summary>
/// One recipe's worth of ingredient lines plus the consumer's desired
/// servings. <see cref="ScalableIngredientLine.RecipeServings"/> is the
/// recipe's NATIVE servings — the ratio for scaling is
/// <c>desired / recipeServings</c>.
/// </summary>
public sealed record IngredientLineMergeInput(
	IReadOnlyList<ScalableIngredientLine> Ingredients,
	int? DesiredServings);

/// <summary>One ingredient line going into the merger.</summary>
public sealed record ScalableIngredientLine(string Name, decimal? Amount, string? Unit, int? RecipeServings);

/// <summary>One merged-and-scaled line coming out of the merger.</summary>
public sealed record MergedIngredientLine(string Name, decimal? Amount, string? Unit);
