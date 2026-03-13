using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class UnitTests
{
    [Fact]
    public void Constructor_ValidUnit_CreatesInstance()
    {
        var unit = new Unit("g");

        unit.Value.Should().Be("g");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new Unit(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var unit = new Unit("  ml  ");

        unit.Value.Should().Be("ml");
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 51);

        var act = () => new Unit(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 50);

        var unit = new Unit(value);

        unit.Value.Should().HaveLength(50);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var unit1 = new Unit("ml");
        var unit2 = new Unit("ml");

        unit1.Should().Be(unit2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var unit1 = new Unit("ml");
        var unit2 = new Unit("g");

        unit1.Should().NotBe(unit2);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var unit = new Unit("cups");

        unit.ToString().Should().Be("cups");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromNullable_NullOrWhitespace_ReturnsNull(string? value)
    {
        Unit.FromNullable(value).Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidValue_ReturnsInstance()
    {
        var result = Unit.FromNullable("ml");

        result.Should().NotBeNull();
        result!.Value.Should().Be("ml");
    }
}
