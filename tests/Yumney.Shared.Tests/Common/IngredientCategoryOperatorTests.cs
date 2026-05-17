using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class IngredientCategoryOperatorTests
{
	[Fact]
	public void ImplicitConversionToString_YieldsValue()
	{
		var category = IngredientCategory.Produce;

		string raw = category;

		raw.Should().Be(category.Value);
	}

	[Fact]
	public void All_ContainsEveryStaticCategoryInOrder()
	{
		IngredientCategory.All.Should().Equal(
			IngredientCategory.Produce,
			IngredientCategory.Dairy,
			IngredientCategory.MeatFish,
			IngredientCategory.Bakery,
			IngredientCategory.Frozen,
			IngredientCategory.Beverages,
			IngredientCategory.Pantry,
			IngredientCategory.Spices,
			IngredientCategory.Household,
			IngredientCategory.Other);
	}
}
