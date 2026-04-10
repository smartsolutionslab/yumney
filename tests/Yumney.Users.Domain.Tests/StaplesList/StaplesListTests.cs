using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.StaplesList;

public class StaplesListTests
{
    private static readonly OwnerIdentifier TestOwner = OwnerIdentifier.From("user-123");

    [Fact]
    public void Create_ValidOwner_ReturnsEmptyList()
    {
        var list = Domain.StaplesList.StaplesList.Create(TestOwner);

        list.Owner.Should().Be(TestOwner);
        list.Items.Should().BeEmpty();
        list.Id.Should().NotBeNull();
    }

    [Fact]
    public void CreateWithDefaults_ValidOwner_ReturnsPrePopulatedList()
    {
        var list = Domain.StaplesList.StaplesList.CreateWithDefaults(TestOwner);

        list.Owner.Should().Be(TestOwner);
        list.Items.Should().NotBeEmpty();
        list.Items.Should().Contain(StapleItem.From("salt"));
        list.Items.Should().Contain(StapleItem.From("pepper"));
        list.Items.Should().Contain(StapleItem.From("butter"));
        list.Items.Should().Contain(StapleItem.From("eggs"));
    }

    [Fact]
    public void CreateWithDefaults_ContainsAllDefaultItems()
    {
        var list = Domain.StaplesList.StaplesList.CreateWithDefaults(TestOwner);

        list.Items.Should().HaveCount(Domain.StaplesList.StaplesList.DefaultItems.Count);
    }

    [Fact]
    public void AddItem_NewItem_AddsToList()
    {
        var list = Domain.StaplesList.StaplesList.Create(TestOwner);

        list.AddItem(StapleItem.From("soy sauce"));

        list.Items.Should().HaveCount(1);
        list.ContainsItem(StapleItem.From("soy sauce")).Should().BeTrue();
    }

    [Fact]
    public void AddItem_DuplicateItem_DoesNotAddTwice()
    {
        var list = Domain.StaplesList.StaplesList.Create(TestOwner);

        list.AddItem(StapleItem.From("salt"));
        list.AddItem(StapleItem.From("salt"));

        list.Items.Should().HaveCount(1);
    }

    [Fact]
    public void AddItem_DuplicateDifferentCase_DoesNotAddTwice()
    {
        var list = Domain.StaplesList.StaplesList.Create(TestOwner);

        list.AddItem(StapleItem.From("Salt"));
        list.AddItem(StapleItem.From("SALT"));

        list.Items.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveItem_ExistingItem_RemovesFromList()
    {
        var list = Domain.StaplesList.StaplesList.CreateWithDefaults(TestOwner);
        var initialCount = list.Items.Count;

        list.RemoveItem(StapleItem.From("butter"));

        list.Items.Should().HaveCount(initialCount - 1);
        list.ContainsItem(StapleItem.From("butter")).Should().BeFalse();
    }

    [Fact]
    public void RemoveItem_NonExistingItem_DoesNothing()
    {
        var list = Domain.StaplesList.StaplesList.Create(TestOwner);
        list.AddItem(StapleItem.From("salt"));

        list.RemoveItem(StapleItem.From("pepper"));

        list.Items.Should().HaveCount(1);
    }

    [Fact]
    public void ContainsItem_ExistingItem_ReturnsTrue()
    {
        var list = Domain.StaplesList.StaplesList.CreateWithDefaults(TestOwner);

        list.ContainsItem(StapleItem.From("salt")).Should().BeTrue();
    }

    [Fact]
    public void ContainsItem_NonExistingItem_ReturnsFalse()
    {
        var list = Domain.StaplesList.StaplesList.Create(TestOwner);

        list.ContainsItem(StapleItem.From("truffle oil")).Should().BeFalse();
    }

    [Fact]
    public void DefaultItems_IsNotEmpty()
    {
        Domain.StaplesList.StaplesList.DefaultItems.Should().NotBeEmpty();
        Domain.StaplesList.StaplesList.DefaultItems.Count.Should().BeGreaterThanOrEqualTo(10);
    }
}
