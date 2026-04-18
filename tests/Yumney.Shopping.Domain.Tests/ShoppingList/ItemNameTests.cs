using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ItemNameTests
{
	[Fact]
	public void Constructor_ValidName_CreatesInstance()
	{
		var name = ItemName.From("Flour");

		name.Value.Should().Be("Flour");
	}

	[Fact]
	public void Constructor_TrimsWhitespace()
	{
		var name = ItemName.From("  Flour  ");

		name.Value.Should().Be("Flour");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => ItemName.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_ExceedsMaxLength_ThrowsGuardException()
	{
		var value = new string('a', 201);

		var act = () => ItemName.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_AtMaxLength_CreatesInstance()
	{
		var value = new string('a', 200);

		var name = ItemName.From(value);

		name.Value.Should().HaveLength(200);
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var name1 = ItemName.From("Flour");
		var name2 = ItemName.From("Flour");

		name1.Should().Be(name2);
	}
}
