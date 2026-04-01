using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping;

[Collection(AspireCollection.Name)]
public class ShoppingListPersistenceTests(AspireFixture fixture) : IAsyncLifetime
{
    private static readonly PagingOptions DefaultPaging = PagingOptions.Of(Page.From(1), PageSize.From(20));
    private static readonly SortingOptions<ShoppingListSortField> DefaultSorting = new(ShoppingListSortField.Date, SortDirection.Descending);

    private readonly OwnerIdentifier owner = OwnerIdentifier.From($"shopping-persist-{Guid.NewGuid():N}");

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var context = await fixture.CreateShoppingDbContextAsync();
        var lists = await context.ShoppingLists
            .Where(l => l.Owner == owner)
            .ToListAsync();
        context.ShoppingLists.RemoveRange(lists);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_NewList_PersistsToDatabase()
    {
        var list = ShoppingListFactory.WeeklyGroceries(owner.Value);

        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(list);
        }

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var saved = await readContext.ShoppingLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == list.Id);

        saved.Should().NotBeNull();
        saved!.Title.Value.Should().Be("Weekly Groceries");
    }

    [Fact]
    public async Task AddAsync_NewList_PersistsItems()
    {
        var list = ShoppingListFactory.WeeklyGroceries(owner.Value);

        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(list);
        }

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var saved = await readContext.ShoppingLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == list.Id);

        saved!.Items.Should().HaveCount(4);
        saved.Items.Select(i => i.Name.Value).Should().Contain("Milk");
    }

    [Fact]
    public async Task AddAsync_NewList_PersistsRecipeReference()
    {
        var list = ShoppingListFactory.BakingIngredients(owner.Value);

        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(list);
        }

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var saved = await readContext.ShoppingLists
            .FirstOrDefaultAsync(l => l.Id == list.Id);

        saved!.RecipeReference.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_WithNullRecipeReference_PersistsNull()
    {
        var list = ShoppingListFactory.WeeklyGroceries(owner.Value);

        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(list);
        }

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var saved = await readContext.ShoppingLists
            .FirstOrDefaultAsync(l => l.Id == list.Id);

        saved!.RecipeReference.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingList_ReturnsWithItems()
    {
        var list = ShoppingListFactory.PartySupplies(owner.Value);

        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(list);
        }

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var repository2 = new ShoppingListRepository(readContext);
        var loaded = await repository2.GetByIdAsync(list.Id);

        loaded.Should().NotBeNull();
        loaded!.Title.Value.Should().Be("Party Supplies");
        loaded.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        await using var context = await fixture.CreateShoppingDbContextAsync();
        var repository = new ShoppingListRepository(context);

        var loaded = await repository.GetByIdAsync(ShoppingListIdentifier.New());

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_ExistingList_ReturnsTrackedEntity()
    {
        var list = ShoppingListFactory.WeeklyGroceries(owner.Value);

        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(list);
        }

        await using var updateContext = await fixture.CreateShoppingDbContextAsync();
        var repository2 = new ShoppingListRepository(updateContext);
        var loaded = await repository2.GetByIdForUpdateAsync(list.Id);

        loaded.Should().NotBeNull();
        updateContext.Entry(loaded!).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task SaveChangesAsync_AfterCheckingItem_PersistsState()
    {
        var list = ShoppingListFactory.WeeklyGroceries(owner.Value);
        var itemId = list.Items[0].Id;

        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(list);
        }

        await using (var updateContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(updateContext);
            var loaded = await repository.GetByIdForUpdateAsync(list.Id);
            loaded!.CheckOffItem(itemId);
            await repository.SaveChangesAsync();
        }

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var repository2 = new ShoppingListRepository(readContext);
        var reloaded = await repository2.GetByIdAsync(list.Id);

        reloaded!.Items.First(i => i.Id == itemId).IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsOnlyOwnersLists()
    {
        var otherOwner = OwnerIdentifier.From($"other-{Guid.NewGuid():N}");

        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(ShoppingListFactory.WeeklyGroceries(owner.Value));
            await repository.AddAsync(ShoppingListFactory.PartySupplies(otherOwner.Value));
        }

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var repository2 = new ShoppingListRepository(readContext);
        var (items, _) = await repository2.GetByOwnerAsync(owner, DefaultPaging, DefaultSorting);

        items.Should().ContainSingle();
        items[0].Title.Value.Should().Be("Weekly Groceries");
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsCorrectTotalCount()
    {
        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(ShoppingListFactory.WeeklyGroceries(owner.Value));
            await repository.AddAsync(ShoppingListFactory.PartySupplies(owner.Value));
            await repository.AddAsync(ShoppingListFactory.BakingIngredients(owner.Value));
        }

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var repository2 = new ShoppingListRepository(readContext);
        var (_, totalCount) = await repository2.GetByOwnerAsync(owner, DefaultPaging, DefaultSorting);

        totalCount.Value.Should().Be(3);
    }

    [Fact]
    public async Task GetByOwnerAsync_WithPagination_RespectsPageSize()
    {
        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(ShoppingListFactory.WeeklyGroceries(owner.Value));
            await repository.AddAsync(ShoppingListFactory.PartySupplies(owner.Value));
            await repository.AddAsync(ShoppingListFactory.BakingIngredients(owner.Value));
        }

        var smallPage = PagingOptions.Of(Page.From(1), PageSize.From(2));

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var repository2 = new ShoppingListRepository(readContext);
        var (items, totalCount) = await repository2.GetByOwnerAsync(owner, smallPage, DefaultSorting);

        items.Should().HaveCount(2);
        totalCount.Value.Should().Be(3);
    }

    [Fact]
    public async Task GetByOwnerAsync_SortByTitleAscending_ReturnsSorted()
    {
        await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
        {
            var repository = new ShoppingListRepository(writeContext);
            await repository.AddAsync(ShoppingListFactory.WeeklyGroceries(owner.Value));
            await repository.AddAsync(ShoppingListFactory.BakingIngredients(owner.Value));
            await repository.AddAsync(ShoppingListFactory.PartySupplies(owner.Value));
        }

        var titleAscSorting = new SortingOptions<ShoppingListSortField>(ShoppingListSortField.Title, SortDirection.Ascending);

        await using var readContext = await fixture.CreateShoppingDbContextAsync();
        var repository2 = new ShoppingListRepository(readContext);
        var (items, _) = await repository2.GetByOwnerAsync(owner, DefaultPaging, titleAscSorting);

        items.Select(i => i.Title.Value).Should().BeInAscendingOrder();
    }
}
