using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.DTOs;

public class ShoppingLedgerMappingExtensionsTests
{
	[Fact]
	public void ToAddedItemDto_ProjectsEveryField()
	{
		var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-1"));
		var quantity = Quantity.Of(Amount.From(2m), Unit.From("kg"));

		var dto = ledger.ToAddedItemDto(
			ItemName.From("Onion"),
			quantity,
			IngredientCategory.From("produce"),
			ItemSource.From("manual"));

		dto.ItemName.Should().Be("Onion");
		dto.Quantity.Should().Be(2m);
		dto.Unit.Should().Be("kg");
		dto.Category.Should().Be("produce");
		dto.Source.Should().Be("manual");
		dto.LedgerIdentifier.Should().Be(ledger.Identifier);
	}

	[Fact]
	public void ToAddedItemDto_NullUnit_StaysNull()
	{
		var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-1"));
		var quantity = Quantity.Of(Amount.From(3m), Unit.FromNullable(null));

		var dto = ledger.ToAddedItemDto(
			ItemName.From("Eggs"),
			quantity,
			IngredientCategory.From("dairy"),
			ItemSource.From("manual"));

		dto.Unit.Should().BeNull();
	}
}
