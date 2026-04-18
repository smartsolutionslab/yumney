using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListTitleTests
{
	[Fact]
	public void Constructor_ValidTitle_CreatesInstance()
	{
		var title = ShoppingListTitle.From("Weekly Groceries");

		title.Value.Should().Be("Weekly Groceries");
	}

	[Fact]
	public void Constructor_TrimsWhitespace()
	{
		var title = ShoppingListTitle.From("  Weekly Groceries  ");

		title.Value.Should().Be("Weekly Groceries");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => ShoppingListTitle.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_ExceedsMaxLength_ThrowsGuardException()
	{
		var value = new string('a', 201);

		var act = () => ShoppingListTitle.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_AtMaxLength_CreatesInstance()
	{
		var value = new string('a', 200);

		var title = ShoppingListTitle.From(value);

		title.Value.Should().HaveLength(200);
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var title1 = ShoppingListTitle.From("Groceries");
		var title2 = ShoppingListTitle.From("Groceries");

		title1.Should().Be(title2);
	}
}
