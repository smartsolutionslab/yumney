using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.DTOs;

public class ShoppingListMappingExtensionsTests
{
	[Fact]
	public void ToDetailDto_ProjectsTopLevelFieldsAndItems()
	{
		var list = ShoppingList.Create(
			ShoppingListTitle.From("Weekly"),
			OwnerIdentifier.From("user-1"),
			[ShoppingListItem.Create(ItemName.From("Onion"), Quantity.Of(Amount.From(2m), Unit.From("kg")))],
			RecipeReference.From(Guid.NewGuid()));

		var dto = list.ToDetailDto();

		dto.Identifier.Should().Be(list.Identifier.Value);
		dto.Title.Should().Be("Weekly");
		dto.RecipeReference.Should().Be(list.RecipeReference!.Value);
		dto.CreatedAt.Should().Be(list.CreatedAt);
		dto.Items.Should().ContainSingle().Which.Name.Should().Be("Onion");
	}

	[Fact]
	public void ToDetailDto_NullRecipeReference_StaysNull()
	{
		var list = ShoppingList.Create(
			ShoppingListTitle.From("Plain"),
			OwnerIdentifier.From("user-1"),
			[ShoppingListItem.Create(ItemName.From("Salt"), quantity: null)],
			recipeReference: null);

		var dto = list.ToDetailDto();

		dto.RecipeReference.Should().BeNull();
	}

	[Fact]
	public void ToSummaryDto_ProjectsAllFields()
	{
		var identifier = ShoppingListIdentifier.From(Guid.NewGuid());
		var createdAt = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
		var summary = new ShoppingListSummary(
			identifier,
			ShoppingListTitle.From("Weekly"),
			ItemCount.From(7),
			createdAt);

		var dto = summary.ToSummaryDto();

		dto.Identifier.Should().Be(identifier.Value);
		dto.Title.Should().Be("Weekly");
		dto.ItemCount.Should().Be(7);
		dto.CreatedAt.Should().Be(createdAt);
	}

	[Fact]
	public void ToSummaryDtos_MapsEverySummary()
	{
		ShoppingListSummary[] summaries =
		[
			new(ShoppingListIdentifier.From(Guid.NewGuid()), ShoppingListTitle.From("A"), ItemCount.From(1), DateTime.UtcNow),
			new(ShoppingListIdentifier.From(Guid.NewGuid()), ShoppingListTitle.From("B"), ItemCount.From(2), DateTime.UtcNow),
		];

		var dtos = summaries.ToSummaryDtos();

		dtos.Should().HaveCount(2);
		dtos[0].Title.Should().Be("A");
		dtos[1].Title.Should().Be("B");
	}

	[Fact]
	public void ItemToDto_WithQuantity_ProjectsAmountAndUnit()
	{
		var item = ShoppingListItem.Create(
			ItemName.From("Flour"),
			Quantity.Of(Amount.From(500m), Unit.From("g")),
			IngredientCategory.From("pantry"));

		var dto = item.ToDto();

		dto.Name.Should().Be("Flour");
		dto.Amount.Should().Be(500m);
		dto.Unit.Should().Be("g");
		dto.Category.Should().Be("pantry");
		dto.IsChecked.Should().BeFalse();
	}

	[Fact]
	public void ItemToDto_WithoutQuantity_LeavesAmountAndUnitNull()
	{
		var item = ShoppingListItem.Create(ItemName.From("Salt"), quantity: null);

		var dto = item.ToDto();

		dto.Amount.Should().BeNull();
		dto.Unit.Should().BeNull();
	}

	[Fact]
	public void ItemsToDtos_MapsEveryItem()
	{
		ShoppingListItem[] items =
		[
			ShoppingListItem.Create(ItemName.From("Onion"), quantity: null),
			ShoppingListItem.Create(ItemName.From("Garlic"), quantity: null),
		];

		var dtos = items.ToDtos();

		dtos.Should().HaveCount(2);
		dtos[0].Name.Should().Be("Onion");
		dtos[1].Name.Should().Be("Garlic");
	}
}
