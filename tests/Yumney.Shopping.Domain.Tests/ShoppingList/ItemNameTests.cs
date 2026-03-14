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
        var name = new ItemName("Flour");

        name.Value.Should().Be("Flour");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var name = new ItemName("  Flour  ");

        name.Value.Should().Be("Flour");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new ItemName(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 201);

        var act = () => new ItemName(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 200);

        var name = new ItemName(value);

        name.Value.Should().HaveLength(200);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var name1 = new ItemName("Flour");
        var name2 = new ItemName("Flour");

        name1.Should().Be(name2);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var name = new ItemName("Flour");

        name.ToString().Should().Be("Flour");
    }
}
