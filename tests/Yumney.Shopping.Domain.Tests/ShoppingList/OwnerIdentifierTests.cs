using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class OwnerIdentifierTests
{
    [Fact]
    public void Constructor_ValidValue_CreatesInstance()
    {
        var owner = OwnerIdentifier.From("user-123");

        owner.Value.Should().Be("user-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => OwnerIdentifier.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var owner = OwnerIdentifier.From("user-123");

        owner.ToString().Should().Be("user-123");
    }
}
