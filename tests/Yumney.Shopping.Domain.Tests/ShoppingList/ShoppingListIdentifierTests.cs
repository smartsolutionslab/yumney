using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListIdentifierTests
{
    [Fact]
    public void Constructor_ValidGuid_CreatesInstance()
    {
        var guid = Guid.NewGuid();

        var identifier = ShoppingListIdentifier.From(guid);

        identifier.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsGuardException()
    {
        var act = () => ShoppingListIdentifier.From(Guid.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();

        var identifier = ShoppingListIdentifier.From(guid);

        identifier.ToString().Should().Be(guid.ToString());
    }
}
