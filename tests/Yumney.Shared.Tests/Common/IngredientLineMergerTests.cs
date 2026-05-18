using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class IngredientLineMergerTests
{
	[Fact]
	public void Merge_SameNameAndUnitAcrossRecipes_SumsAmounts()
	{
		var merged = IngredientLineMerger.Merge(
		[
			Input(servings: 4, ("Flour", 200m, "g")),
			Input(servings: 4, ("Flour", 300m, "g")),
		]);

		merged.Should().ContainSingle();
		merged[0].Name.Should().Be("Flour");
		merged[0].Amount.Should().Be(500m);
		merged[0].Unit.Should().Be("g");
	}

	[Fact]
	public void Merge_SameNameDifferentUnits_KeepsSeparate()
	{
		var merged = IngredientLineMerger.Merge(
		[
			Input(servings: 4, ("Milk", 2m, "cup")),
			Input(servings: 4, ("Milk", 500m, "ml")),
		]);

		merged.Should().HaveCount(2);
		merged.Should().ContainSingle(line => line.Unit == "cup" && line.Amount == 2m);
		merged.Should().ContainSingle(line => line.Unit == "ml" && line.Amount == 500m);
	}

	[Fact]
	public void Merge_DifferentCaseSameIngredient_TreatsAsDuplicate()
	{
		var merged = IngredientLineMerger.Merge(
		[
			Input(servings: 4, ("flour", 200m, "g")),
			Input(servings: 4, ("FLOUR", 300m, "G")),
		]);

		merged.Should().ContainSingle();
		merged[0].Amount.Should().Be(500m);
	}

	[Fact]
	public void Merge_ScalesAmountsToDesiredServings()
	{
		var merged = IngredientLineMerger.Merge(
		[
			InputWithDesired(recipeServings: 4, desiredServings: 8, ("Flour", 100m, "g")),
		]);

		merged.Should().ContainSingle();
		merged[0].Amount.Should().Be(200m);
	}

	[Fact]
	public void Merge_DesiredServingsNull_DoesNotScale()
	{
		var merged = IngredientLineMerger.Merge(
		[
			InputWithDesired(recipeServings: 4, desiredServings: null, ("Flour", 100m, "g")),
		]);

		merged[0].Amount.Should().Be(100m);
	}

	[Fact]
	public void Merge_RecipeServingsZeroOrNull_DoesNotScale()
	{
		var merged = IngredientLineMerger.Merge(
		[
			InputWithDesired(recipeServings: 0, desiredServings: 8, ("Flour", 100m, "g")),
			InputWithDesired(recipeServings: null, desiredServings: 8, ("Sugar", 50m, "g")),
		]);

		merged.Should().Contain(line => line.Name == "Flour" && line.Amount == 100m);
		merged.Should().Contain(line => line.Name == "Sugar" && line.Amount == 50m);
	}

	[Fact]
	public void Merge_IntegerAmountStaysIntegerAfterScale()
	{
		// 3 eggs × (2/4) = 1.5 → integer-original rounds to whole.
		var merged = IngredientLineMerger.Merge(
		[
			InputWithDesired(recipeServings: 4, desiredServings: 2, ("Eggs", 3m, null)),
		]);

		merged[0].Amount.Should().Be(2m); // Math.Round(1.5) = 2 (banker's rounding to even).
	}

	[Fact]
	public void Merge_FractionalAmountRoundsToTwoDecimalPlaces()
	{
		// 2.5 cups × (4/2) = 5 — but the original amount is fractional so the
		// rounding path uses 2 decimal places. Going the other way: 1.25 cups ×
		// (3/2) = 1.875 → Math.Round(1.875, 2) = 1.88 (banker's-rounding
		// rounds-half-to-even still rounds the trailing .005 to .88).
		var merged = IngredientLineMerger.Merge(
		[
			InputWithDesired(recipeServings: 2, desiredServings: 3, ("Milk", 1.25m, "cup")),
		]);

		merged[0].Amount.Should().Be(1.88m);
	}

	[Fact]
	public void Merge_AllNullAmountsForOneLine_KeepsAmountNull()
	{
		var merged = IngredientLineMerger.Merge(
		[
			Input(servings: 4, ("Salt", null, null)),
			Input(servings: 4, ("salt", null, null)),
		]);

		merged.Should().ContainSingle();
		merged[0].Amount.Should().BeNull();
	}

	[Fact]
	public void Merge_NullAndNonNullAmountForSameLine_SumsTheNonNullOnly()
	{
		// Edge case the legacy MealPlan handler got wrong: it treated null as 0,
		// which polluted the sum. The shared helper preserves the non-null
		// value and ignores the null contribution.
		var merged = IngredientLineMerger.Merge(
		[
			Input(servings: 4, ("Pepper", null, null)),
			Input(servings: 4, ("Pepper", 2m, null)),
		]);

		merged.Should().ContainSingle();
		merged[0].Amount.Should().Be(2m);
	}

	[Fact]
	public void Merge_NameWithSurroundingWhitespace_NormalisesByTrim()
	{
		var merged = IngredientLineMerger.Merge(
		[
			Input(servings: 4, ("  Flour", 100m, "g")),
			Input(servings: 4, ("Flour  ", 200m, "g")),
		]);

		merged.Should().ContainSingle();
		merged[0].Amount.Should().Be(300m);
	}

	[Fact]
	public void Merge_EmptyInput_ReturnsEmpty()
	{
		var merged = IngredientLineMerger.Merge([]);

		merged.Should().BeEmpty();
	}

	private static IngredientLineMergeInput Input(int servings, params (string Name, decimal? Amount, string? Unit)[] ingredients) =>
		InputWithDesired(servings, servings, ingredients);

	private static IngredientLineMergeInput InputWithDesired(
		int? recipeServings,
		int? desiredServings,
		params (string Name, decimal? Amount, string? Unit)[] ingredients) =>
		new(
			[.. ingredients.Select(ingredient => new ScalableIngredientLine(
				ingredient.Name,
				ingredient.Amount,
				ingredient.Unit,
				recipeServings))],
			desiredServings);
}
