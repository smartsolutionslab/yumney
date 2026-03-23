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

        shoppingList.Id.Should().NotBeNull();
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
        var owner = OwnerIdentifier.From("user-123");

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
    public void Create_WithRecipeReference_SetsRecipeReference()
    {
        var recipeReference = RecipeReference.From(Guid.NewGuid());

        var shoppingList = CreateValidShoppingList(recipeReference: recipeReference);

        shoppingList.RecipeReference.Should().Be(recipeReference);
    }

    [Fact]
    public void Create_WithoutRecipeReference_RecipeReferenceIsNull()
    {
        var shoppingList = CreateValidShoppingList();

        shoppingList.RecipeReference.Should().BeNull();
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

        domainEvent.ShoppingListIdentifier.Should().Be(shoppingList.Id);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var list1 = CreateValidShoppingList();
        var list2 = CreateValidShoppingList();

        list1.Id.Should().NotBe(list2.Id);
    }

    [Fact]
    public void CheckOffItem_ChecksTheItem()
    {
        var items = new List<ShoppingListItem>
        {
            ShoppingListItem.Create(new ItemName("Flour"), new Amount(500), new Unit("g")),
        };
        var shoppingList = CreateValidShoppingList(items: items);

        shoppingList.CheckOffItem(items[0].Id);

        shoppingList.Items[0].IsChecked.Should().BeTrue();
    }

    [Fact]
    public void UncheckItem_UnchecksTheItem()
    {
        var items = new List<ShoppingListItem>
        {
            ShoppingListItem.Create(new ItemName("Flour"), new Amount(500), new Unit("g")),
        };
        var shoppingList = CreateValidShoppingList(items: items);
        shoppingList.CheckOffItem(items[0].Id);

        shoppingList.UncheckItem(items[0].Id);

        shoppingList.Items[0].IsChecked.Should().BeFalse();
    }

    [Fact]
    public void CheckAllItems_ChecksAllItems()
    {
        var items = new List<ShoppingListItem>
        {
            ShoppingListItem.Create(new ItemName("Flour"), new Amount(500), new Unit("g")),
            ShoppingListItem.Create(new ItemName("Sugar"), new Amount(200), new Unit("g")),
        };
        var shoppingList = CreateValidShoppingList(items: items);

        shoppingList.CheckAllItems();

        shoppingList.Items.Should().OnlyContain(i => i.IsChecked);
    }

    [Fact]
    public void UncheckAllItems_UnchecksAllItems()
    {
        var items = new List<ShoppingListItem>
        {
            ShoppingListItem.Create(new ItemName("Flour"), new Amount(500), new Unit("g")),
            ShoppingListItem.Create(new ItemName("Sugar"), new Amount(200), new Unit("g")),
        };
        var shoppingList = CreateValidShoppingList(items: items);
        shoppingList.CheckAllItems();

        shoppingList.UncheckAllItems();

        shoppingList.Items.Should().OnlyContain(i => !i.IsChecked);
    }

    [Fact]
    public void CheckOffItem_InvalidItemId_Throws()
    {
        var shoppingList = CreateValidShoppingList();

        var act = () => shoppingList.CheckOffItem(Guid.NewGuid());

        act.Should().Throw<GuardException>();
    }

    private static Domain.ShoppingList.ShoppingList CreateValidShoppingList(
        ShoppingListTitle? title = null,
        OwnerIdentifier? owner = null,
        IReadOnlyList<ShoppingListItem>? items = null,
        RecipeReference? recipeReference = null)
    {
        return Domain.ShoppingList.ShoppingList.Create(
            title ?? new ShoppingListTitle("Test Shopping List"),
            owner ?? OwnerIdentifier.From("user-123"),
            items ?? [ShoppingListItem.Create(new ItemName("Flour"), new Amount(500), new Unit("g"))],
            recipeReference);
    }
}
