using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class OwnerIdentifierTests
{
    [Fact]
    public void From_ValidValue_CreatesInstance()
    {
        var owner = OwnerIdentifier.From("user-123");

        owner.Value.Should().Be("user-123");
    }

    [Fact]
    public void From_TrimsWhitespace()
    {
        var owner = OwnerIdentifier.From("  user-123  ");

        owner.Value.Should().Be("user-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => OwnerIdentifier.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 256);

        var act = () => OwnerIdentifier.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 255);

        var owner = OwnerIdentifier.From(value);

        owner.Value.Should().HaveLength(255);
    }
}
