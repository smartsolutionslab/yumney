using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListSummaryTests
{
	[Fact]
	public void PositionalCtor_StampsAllFields()
	{
		var identifier = ShoppingListIdentifier.From(Guid.NewGuid());
		var title = ShoppingListTitle.From("Weekly groceries");
		var itemCount = ItemCount.From(7);
		var createdAt = new DateTime(2026, 5, 17, 8, 30, 0, DateTimeKind.Utc);

		var summary = new ShoppingListSummary(identifier, title, itemCount, createdAt);

		summary.Identifier.Should().Be(identifier);
		summary.Title.Should().Be(title);
		summary.ItemCount.Should().Be(itemCount);
		summary.CreatedAt.Should().Be(createdAt);
	}

	[Fact]
	public void Equality_SameFields_AreEqual()
	{
		var identifier = ShoppingListIdentifier.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
		var createdAt = new DateTime(2026, 5, 17, 8, 30, 0, DateTimeKind.Utc);

		var a = new ShoppingListSummary(identifier, ShoppingListTitle.From("A"), ItemCount.From(3), createdAt);
		var b = new ShoppingListSummary(identifier, ShoppingListTitle.From("A"), ItemCount.From(3), createdAt);

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	[Fact]
	public void WithExpression_ChangesOnlyTargetField()
	{
		var original = new ShoppingListSummary(
			ShoppingListIdentifier.From(Guid.NewGuid()),
			ShoppingListTitle.From("Original"),
			ItemCount.From(2),
			DateTime.UtcNow);

		var renamed = original with { Title = ShoppingListTitle.From("Renamed") };

		renamed.Title.Value.Should().Be("Renamed");
		renamed.Identifier.Should().Be(original.Identifier);
		renamed.ItemCount.Should().Be(original.ItemCount);
		renamed.CreatedAt.Should().Be(original.CreatedAt);
	}
}
