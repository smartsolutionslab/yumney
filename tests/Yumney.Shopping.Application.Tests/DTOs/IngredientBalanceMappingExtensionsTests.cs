using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.DTOs;

public class IngredientBalanceMappingExtensionsTests
{
	[Fact]
	public void ToStapleBalanceItem_ProjectsCategoryAndNameWithStapleSource()
	{
		var category = IngredientCategory.From("pantry");

		var item = category.ToStapleBalanceItem("Flour");

		item.ItemName.Should().Be("Flour");
		item.Category.Should().Be("pantry");
		item.Source.Should().Be(IngredientBalanceSource.Staple);
		item.Quantity.Should().BeNull();
		item.Unit.Should().BeNull();
		item.Freshness.Should().Be(Freshness.NotTracked);
	}

	[Fact]
	public void ToBalanceDto_WrapsItemListInBalanceDto()
	{
		IReadOnlyList<IngredientBalanceItemDto> items =
		[
			IngredientCategory.From("pantry").ToStapleBalanceItem("Flour"),
			IngredientCategory.From("spices").ToStapleBalanceItem("Salt"),
		];

		var dto = items.ToBalanceDto();

		dto.Items.Should().BeSameAs(items);
	}

	[Fact]
	public void ToBalanceDto_EmptyItems_YieldsDtoWithEmptyItems()
	{
		IReadOnlyList<IngredientBalanceItemDto> items = [];

		var dto = items.ToBalanceDto();

		dto.Items.Should().BeEmpty();
	}
}
