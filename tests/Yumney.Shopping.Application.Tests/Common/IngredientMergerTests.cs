using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Application.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Common;

public class IngredientMergerTests
{
	[Fact]
	public void Merge_SameNameAndUnitAcrossRecipes_SumsAmounts()
	{
		var inputs = new[]
		{
			Input(servings: 4, ("Flour", 200m, "g")),
			Input(servings: 4, ("Flour", 300m, "g")),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged.Should().ContainSingle();
		merged[0].Name.Value.Should().Be("Flour");
		merged[0].Quantity!.Amount.Value.Should().Be(500);
		merged[0].Quantity!.Unit!.Value.Should().Be("g");
	}

	[Fact]
	public void Merge_SameNameDifferentUnits_KeepsSeparate()
	{
		var inputs = new[]
		{
			Input(servings: 4, ("Milk", 2m, "cup")),
			Input(servings: 4, ("Milk", 500m, "ml")),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged.Should().HaveCount(2);
		merged.Should().ContainSingle(merge => merge.Quantity!.Unit!.Value == "cup" && merge.Quantity!.Amount.Value == 2);
		merged.Should().ContainSingle(merge => merge.Quantity!.Unit!.Value == "ml" && merge.Quantity!.Amount.Value == 500);
	}

	[Fact]
	public void Merge_DifferentCaseSameIngredient_TreatsAsDuplicate()
	{
		var inputs = new[]
		{
			Input(servings: 4, ("flour", 200m, "g")),
			Input(servings: 4, ("FLOUR", 300m, "G")),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged.Should().ContainSingle();
		merged[0].Quantity!.Amount.Value.Should().Be(500);
	}

	[Fact]
	public void Merge_ScalesAmountsToDesiredServings()
	{
		var inputs = new[]
		{
			InputWithDesired(originalServings: 4, desiredServings: 6, ("Flour", 200m, "g")),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged.Should().ContainSingle();
		merged[0].Quantity!.Amount.Value.Should().Be(300);
	}

	[Fact]
	public void Merge_ScalesDownAndRoundsIntegersToInteger()
	{
		var inputs = new[]
		{
			InputWithDesired(originalServings: 4, desiredServings: 1, ("Eggs", 4m, null)),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged.Should().ContainSingle();
		merged[0].Quantity!.Amount.Value.Should().Be(1);
	}

	[Fact]
	public void Merge_ScalesNonIntegerAmountsToTwoDecimals()
	{
		var inputs = new[]
		{
			InputWithDesired(originalServings: 4, desiredServings: 6, ("Olive Oil", 1.5m, "tbsp")),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged.Should().ContainSingle();
		merged[0].Quantity!.Amount.Value.Should().Be(2.25m);
	}

	[Fact]
	public void Merge_NullAmountAcrossAllOccurrences_ProducesNullQuantity()
	{
		var inputs = new[]
		{
			Input(servings: 4, ("Salt", null, null)),
			Input(servings: 4, ("Salt", null, null)),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged.Should().ContainSingle();
		merged[0].Quantity.Should().BeNull();
	}

	[Fact]
	public void Merge_MissingDesiredServings_DoesNotScale()
	{
		var inputs = new[]
		{
			new IngredientMergeInput(
				new[] { new RecipeIngredientLookupResult("Flour", 200m, "g", 4) },
				DesiredServings: null),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged[0].Quantity!.Amount.Value.Should().Be(200);
	}

	[Fact]
	public void Merge_MissingOriginalServings_DoesNotScale()
	{
		var inputs = new[]
		{
			new IngredientMergeInput(
				new[] { new RecipeIngredientLookupResult("Flour", 200m, "g", null) },
				Servings.From(8)),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged[0].Quantity!.Amount.Value.Should().Be(200);
	}

	[Fact]
	public void Merge_MultipleRecipesWithMixedOverlap_MergesCorrectly()
	{
		var inputs = new[]
		{
			InputWithDesired(originalServings: 4, desiredServings: 4, ("Flour", 200m, "g"), ("Eggs", 2m, null)),
			InputWithDesired(originalServings: 4, desiredServings: 4, ("Flour", 300m, "g"), ("Milk", 250m, "ml")),
			InputWithDesired(originalServings: 4, desiredServings: 4, ("Salt", null, null)),
		};

		var merged = IngredientMerger.Merge(inputs);

		merged.Should().HaveCount(4);
		merged.Should().ContainSingle(merge => merge.Name.Value == "Flour" && merge.Quantity!.Amount.Value == 500);
		merged.Should().ContainSingle(merge => merge.Name.Value == "Eggs" && merge.Quantity!.Amount.Value == 2);
		merged.Should().ContainSingle(merge => merge.Name.Value == "Milk" && merge.Quantity!.Amount.Value == 250);
		merged.Should().ContainSingle(merge => merge.Name.Value == "Salt" && merge.Quantity == null);
	}

	private static IngredientMergeInput Input(int servings, params (string Name, decimal? Amount, string? Unit)[] ingredients)
	{
		return new IngredientMergeInput(
			ingredients.Select(item => new RecipeIngredientLookupResult(item.Name, item.Amount, item.Unit, servings)).ToList(),
			Servings.From(servings));
	}

	private static IngredientMergeInput InputWithDesired(
		int originalServings,
		int desiredServings,
		params (string Name, decimal? Amount, string? Unit)[] ingredients)
	{
		return new IngredientMergeInput(
			ingredients.Select(item => new RecipeIngredientLookupResult(item.Name, item.Amount, item.Unit, originalServings)).ToList(),
			Servings.From(desiredServings));
	}
}
