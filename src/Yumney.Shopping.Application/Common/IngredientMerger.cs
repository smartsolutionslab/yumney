using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Common;

public static class IngredientMerger
{
	public static IReadOnlyList<MergedIngredient> Merge(IEnumerable<IngredientMergeInput> inputs)
	{
		var scaled = inputs.SelectMany(Scale).ToList();

		var groupedByKey = scaled
			.GroupBy(scaledIngredient => new MergeKey(
				scaledIngredient.Name.Trim(),
				scaledIngredient.Unit?.Trim()))
			.Select(BuildMerged)
			.ToList();

		return groupedByKey;
	}

	private static IEnumerable<ScaledIngredient> Scale(IngredientMergeInput input)
	{
		foreach (var ingredient in input.Ingredients)
		{
			yield return new ScaledIngredient(
				ingredient.Name,
				ScaleAmount(ingredient.Amount, ingredient.RecipeServings, input.DesiredServings),
				ingredient.Unit);
		}
	}

	private static decimal? ScaleAmount(decimal? amount, int? originalServings, Servings? desiredServings)
	{
		if (amount is null) return null;
		if (desiredServings is null || originalServings is null or <= 0) return amount;
		if (originalServings == desiredServings.Value) return amount;

		var ratio = (decimal)desiredServings.Value / originalServings.Value;
		var scaled = amount.Value * ratio;
		return amount.Value % 1 == 0
			? Math.Round(scaled)
			: Math.Round(scaled, 2);
	}

	private static MergedIngredient BuildMerged(IGrouping<MergeKey, ScaledIngredient> group)
	{
		var first = group.First();
		var name = ItemName.From(first.Name);
		var unit = Unit.FromNullable(first.Unit);

		var summed = group
			.Select(item => item.Amount)
			.Where(amount => amount.HasValue)
			.Sum(amount => amount!.Value);
		if (summed == 0 && group.All(item => item.Amount is null))
		{
			return new MergedIngredient(name, null);
		}

		return new MergedIngredient(name, Quantity.Of(Amount.From(summed), unit));
	}

	private sealed record ScaledIngredient(string Name, decimal? Amount, string? Unit);

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
