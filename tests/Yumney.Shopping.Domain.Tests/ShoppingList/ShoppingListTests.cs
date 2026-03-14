using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListTests
{
    [Fact]
    public void Create_ValidInput_CreatesShoppingListWithId()
    {
        var shoppingList = CreateValidShoppingList();

        shoppingList.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ValidInput_SetsTitle()
    {
        var title = new ShoppingListTitle("Weekly Groceries");

        var shoppingList = CreateValidShoppingList(title: title);

        shoppingList.Title.Should().Be(title);
    }

    [Fact]
    public void Create_ValidInput_SetsOwner()
    {
        var owner = new OwnerIdentifier("user-123");

        var shoppingList = CreateValidShoppingList(owner: owner);

        shoppingList.Owner.Should().Be(owner);
    }

    [Fact]
    public void Create_ValidInput_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;

        var shoppingList = CreateValidShoppingList();

        shoppingList.CreatedAt.Should().BeOnOrAfter(before);
        shoppingList.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Create_ValidInput_SetsItems()
    {
        var items = new List<ShoppingListItem>
        {
            ShoppingListItem.Create(new ItemName("Flour"), new Amount(500), new Unit("g")),
            ShoppingListItem.Create(new ItemName("Sugar"), new Amount(200), new Unit("g")),
        };

        var shoppingList = CreateValidShoppingList(items: items);

        shoppingList.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithRecipeIdentifier_SetsRecipeIdentifier()
    {
        var recipeId = Guid.NewGuid();

        var shoppingList = CreateValidShoppingList(recipeIdentifier: recipeId);

        shoppingList.RecipeIdentifier.Should().Be(recipeId);
    }

    [Fact]
    public void Create_WithoutRecipeIdentifier_RecipeIdentifierIsNull()
    {
        var shoppingList = CreateValidShoppingList();

        shoppingList.RecipeIdentifier.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyItems_ThrowsGuardException()
    {
        var act = () => CreateValidShoppingList(items: []);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Create_RaisesShoppingListCreatedEvent()
    {
        var title = new ShoppingListTitle("Groceries");

        var shoppingList = CreateValidShoppingList(title: title);

        shoppingList.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ShoppingListCreatedEvent>()
            .Which.Title.Should().Be(title);
    }

    [Fact]
    public void Create_ShoppingListCreatedEvent_ContainsShoppingListId()
    {
        var shoppingList = CreateValidShoppingList();

        var domainEvent = shoppingList.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ShoppingListCreatedEvent>().Subject;

        domainEvent.ShoppingListIdentifier.Value.Should().Be(shoppingList.Id);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var list1 = CreateValidShoppingList();
        var list2 = CreateValidShoppingList();

        list1.Id.Should().NotBe(list2.Id);
    }

    private static Domain.ShoppingList.ShoppingList CreateValidShoppingList(
        ShoppingListTitle? title = null,
        OwnerIdentifier? owner = null,
        IReadOnlyList<ShoppingListItem>? items = null,
        Guid? recipeIdentifier = null)
    {
        return Domain.ShoppingList.ShoppingList.Create(
            title ?? new ShoppingListTitle("Test Shopping List"),
            owner ?? new OwnerIdentifier("user-123"),
            items ?? [ShoppingListItem.Create(new ItemName("Flour"), new Amount(500), new Unit("g"))],
            recipeIdentifier);
    }
}
