using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListItemIdentifierTests
{
    [Fact]
    public void From_ValidGuid_CreatesInstance()
    {
        var guid = Guid.NewGuid();

        var identifier = ShoppingListItemIdentifier.From(guid);

        identifier.Value.Should().Be(guid);
    }

    [Fact]
    public void From_EmptyGuid_ThrowsGuardException()
    {
        var act = () => ShoppingListItemIdentifier.From(Guid.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void New_GeneratesUniqueId()
    {
        var first = ShoppingListItemIdentifier.New();
        var second = ShoppingListItemIdentifier.New();

        first.Should().NotBe(second);
    }

    [Fact]
    public void New_IsNotEmpty()
    {
        var identifier = ShoppingListItemIdentifier.New();

        identifier.Value.Should().NotBeEmpty();
    }
}
