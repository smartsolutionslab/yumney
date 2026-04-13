using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class UnitTests
{
    [Fact]
    public void Constructor_ValidUnit_CreatesInstance()
    {
        var unit = Unit.From("kg");

        unit.Value.Should().Be("kg");
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 51);

        var act = () => Unit.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 50);

        var unit = Unit.From(value);

        unit.Value.Should().HaveLength(50);
    }

    [Fact]
    public void FromNullable_WithValue_CreatesInstance()
    {
        var unit = Unit.FromNullable("ml");

        unit.Should().NotBeNull();
        unit!.Value.Should().Be("ml");
    }

    [Fact]
    public void FromNullable_Null_ReturnsNull()
    {
        var unit = Unit.FromNullable(null);

        unit.Should().BeNull();
    }

    [Fact]
    public void FromNullable_Whitespace_ReturnsNull()
    {
        var unit = Unit.FromNullable("   ");

        unit.Should().BeNull();
    }
}
