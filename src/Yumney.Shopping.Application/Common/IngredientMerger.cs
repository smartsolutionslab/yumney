using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Common;

/// <summary>
/// Shopping-flavoured façade over <see cref="IngredientLineMerger"/>.
/// Coerces Shopping's domain types (RecipeIngredientLookupResult, Servings,
/// ItemName, Quantity) onto the primitives the shared helper accepts, runs
/// the merge, and wraps the result back into <see cref="MergedIngredient"/>.
/// All merge math (scaling, case-insensitive grouping, null-amount handling,
/// smart rounding) lives in Shared.Quantities so MealPlan can apply the
/// same semantics.
/// </summary>
public static class IngredientMerger
{
	public static IReadOnlyList<MergedIngredient> Merge(IEnumerable<IngredientMergeInput> inputs) =>
		IngredientLineMerger
			.Merge(inputs.Select(ToLineInput))
			.Select(ToMergedIngredient)
			.ToList();

	private static IngredientLineMergeInput ToLineInput(IngredientMergeInput input) =>
		new(
			[.. input.Ingredients.Select(ingredient => new ScalableIngredientLine(
				ingredient.Name,
				ingredient.Amount,
				ingredient.Unit,
				ingredient.RecipeServings))],
			input.DesiredServings?.Value);

	private static MergedIngredient ToMergedIngredient(MergedIngredientLine line)
	{
		var name = ItemName.From(line.Name);
		if (line.Amount is null)
		{
			return new MergedIngredient(name, Quantity: null);
		}

		return new MergedIngredient(name, Quantity.Of(Amount.From(line.Amount.Value), Unit.FromNullable(line.Unit)));
	}
}
