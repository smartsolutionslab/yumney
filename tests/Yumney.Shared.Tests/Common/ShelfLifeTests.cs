using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class ShelfLifeTests
{
	[Theory]
	[InlineData("meat-fish", 2)]
	[InlineData("dairy", 6)]
	[InlineData("produce", 5)]
	[InlineData("bakery", 3)]
	[InlineData("frozen", 60)]
	public void DaysFor_PerishableCategory_ReturnsExpected(string category, int expected)
	{
		ShelfLife.DaysFor(IngredientCategory.From(category)).Should().Be(expected);
	}

	[Theory]
	[InlineData("pantry")]
	[InlineData("household")]
	[InlineData("beverages")]
	[InlineData("other")]
	public void DaysFor_NonTrackedCategory_ReturnsNull(string category)
	{
		ShelfLife.DaysFor(IngredientCategory.From(category)).Should().BeNull();
	}

	[Fact]
	public void Classify_NullDaysSinceBought_AlwaysNotTracked()
	{
		ShelfLife.Classify(IngredientCategory.MeatFish, null).Should().Be(Freshness.NotTracked);
	}

	[Fact]
	public void Classify_NonTrackedCategory_ReturnsNotTrackedRegardlessOfAge()
	{
		ShelfLife.Classify(IngredientCategory.Pantry, 30).Should().Be(Freshness.NotTracked);
	}

	[Theory]
	[InlineData(0, Freshness.Fresh)] // 0 days < 1 (= 50% of 2)
	[InlineData(1, Freshness.UseSoon)] // 1 day = 50% of 2
	[InlineData(2, Freshness.CheckIt)] // expired
	[InlineData(5, Freshness.CheckIt)]
	public void Classify_MeatFish_TwoDayShelfLife(int daysSinceBought, Freshness expected)
	{
		ShelfLife.Classify(IngredientCategory.MeatFish, daysSinceBought).Should().Be(expected);
	}

	[Theory]
	[InlineData(0, Freshness.Fresh)]
	[InlineData(2, Freshness.Fresh)] // 2*2 = 4 < 6 → Fresh
	[InlineData(3, Freshness.UseSoon)] // 3*2 = 6 >= 6 → UseSoon
	[InlineData(5, Freshness.UseSoon)]
	[InlineData(6, Freshness.CheckIt)]
	public void Classify_Dairy_SixDayShelfLife(int daysSinceBought, Freshness expected)
	{
		ShelfLife.Classify(IngredientCategory.Dairy, daysSinceBought).Should().Be(expected);
	}

	[Theory]
	[InlineData(29, Freshness.Fresh)] // 29*2 = 58 < 60
	[InlineData(30, Freshness.UseSoon)] // 30*2 = 60 >= 60
	[InlineData(60, Freshness.CheckIt)]
	public void Classify_Frozen_SixtyDayShelfLife(int daysSinceBought, Freshness expected)
	{
		ShelfLife.Classify(IngredientCategory.Frozen, daysSinceBought).Should().Be(expected);
	}
}
