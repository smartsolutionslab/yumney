using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class OwnerIdentifierTests
{
    [Fact]
    public void Constructor_ValidValue_CreatesInstance()
    {
        var owner = new OwnerIdentifier("user-123");

        owner.Value.Should().Be("user-123");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var owner = new OwnerIdentifier("  user-123  ");

        owner.Value.Should().Be("user-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new OwnerIdentifier(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 256);

        var act = () => new OwnerIdentifier(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 255);

        var owner = new OwnerIdentifier(value);

        owner.Value.Should().HaveLength(255);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var owner = new OwnerIdentifier("user-123");

        owner.ToString().Should().Be("user-123");
    }
}
