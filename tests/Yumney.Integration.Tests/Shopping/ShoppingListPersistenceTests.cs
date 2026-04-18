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

	public Task DisposeAsync() => AspireFixture.CleanupAsync(
		fixture.CreateShoppingDbContextAsync,
		ctx => ctx.ShoppingLists.Where(l => l.Owner == owner));

	[Fact]
	public async Task AddAsync_NewList_PersistsWithItems()
	{
		var list = ShoppingListFactory.WeeklyGroceries(owner.Value);

		await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
		{
			var shoppingLists = new ShoppingListRepository(writeContext);
			await shoppingLists.AddAsync(list);
		}

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var saved = await readContext.ShoppingLists
			.Include(l => l.Items)
			.FirstOrDefaultAsync(l => l.Id == list.Id);

		saved.Should().NotBeNull();
		saved!.Title.Value.Should().Be("Weekly Groceries");
		saved.Items.Should().HaveCount(4);
		saved.Items.Select(i => i.Name.Value).Should().Contain("Milk");
		saved.RecipeReference.Should().BeNull();
	}

	[Fact]
	public async Task AddAsync_WithRecipeReference_PersistsReference()
	{
		var list = ShoppingListFactory.BakingIngredients(owner.Value);

		await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
		{
			var shoppingLists = new ShoppingListRepository(writeContext);
			await shoppingLists.AddAsync(list);
		}

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var saved = await readContext.ShoppingLists
			.FirstOrDefaultAsync(l => l.Id == list.Id);

		saved!.RecipeReference.Should().NotBeNull();
	}

	[Fact]
	public async Task GetByIdAsync_ExistingList_ReturnsWithItems()
	{
		var list = ShoppingListFactory.PartySupplies(owner.Value);
		await fixture.SeedShoppingListsAsync(list);

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(readContext);
		var loaded = await shoppingLists.GetByIdAsync(list.Id);

		loaded.Title.Value.Should().Be("Party Supplies");
		loaded.Items.Should().NotBeEmpty();
	}

	[Fact]
	public async Task GetByIdAsync_NonExistent_ThrowsEntityNotFoundException()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(context);

		var act = () => shoppingLists.GetByIdAsync(ShoppingListIdentifier.New());

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task GetByIdForUpdateAsync_ExistingList_ReturnsTrackedEntity()
	{
		var list = ShoppingListFactory.WeeklyGroceries(owner.Value);
		await fixture.SeedShoppingListsAsync(list);

		await using var updateContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(updateContext);
		var loaded = await shoppingLists.GetByIdForUpdateAsync(list.Id);

		updateContext.Entry(loaded).State.Should().Be(EntityState.Unchanged);
	}

	[Fact]
	public async Task SaveChangesAsync_AfterCheckingItem_PersistsState()
	{
		var list = ShoppingListFactory.WeeklyGroceries(owner.Value);
		var itemId = list.Items[0].Id;
		await fixture.SeedShoppingListsAsync(list);

		await using (var updateContext = await fixture.CreateShoppingDbContextAsync())
		{
			var shoppingLists = new ShoppingListRepository(updateContext);
			var loaded = await shoppingLists.GetByIdForUpdateAsync(list.Id);
			loaded.CheckOffItem(itemId);
			await shoppingLists.SaveChangesAsync();
		}

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists2 = new ShoppingListRepository(readContext);
		var reloaded = await shoppingLists2.GetByIdAsync(list.Id);

		reloaded.Items.First(i => i.Id == itemId).IsChecked.Should().BeTrue();
	}

	[Fact]
	public async Task GetByOwnerAsync_ReturnsOnlyOwnersLists()
	{
		var otherOwner = OwnerIdentifier.From($"other-{Guid.NewGuid():N}");

		await using (var writeContext = await fixture.CreateShoppingDbContextAsync())
		{
			var shoppingLists = new ShoppingListRepository(writeContext);
			await shoppingLists.AddAsync(ShoppingListFactory.WeeklyGroceries(owner.Value));
			await shoppingLists.AddAsync(ShoppingListFactory.PartySupplies(otherOwner.Value));
		}

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists2 = new ShoppingListRepository(readContext);
		var (items, _) = await shoppingLists2.GetByOwnerAsync(owner, DefaultPaging, DefaultSorting);

		items.Should().ContainSingle();
		items[0].Title.Value.Should().Be("Weekly Groceries");
	}

	[Fact]
	public async Task GetByOwnerAsync_WithPagination_RespectsPageSizeAndTotalCount()
	{
		await fixture.SeedShoppingListsAsync(
			ShoppingListFactory.WeeklyGroceries(owner.Value),
			ShoppingListFactory.PartySupplies(owner.Value),
			ShoppingListFactory.BakingIngredients(owner.Value));

		var smallPage = PagingOptions.Of(Page.From(1), PageSize.From(2));

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(readContext);
		var (items, totalCount) = await shoppingLists.GetByOwnerAsync(owner, smallPage, DefaultSorting);

		items.Should().HaveCount(2);
		totalCount.Value.Should().Be(3);
	}

	[Fact]
	public async Task GetByOwnerAsync_SortByTitleAscending_ReturnsSorted()
	{
		await fixture.SeedShoppingListsAsync(
			ShoppingListFactory.WeeklyGroceries(owner.Value),
			ShoppingListFactory.BakingIngredients(owner.Value),
			ShoppingListFactory.PartySupplies(owner.Value));
		var sorting = new SortingOptions<ShoppingListSortField>(ShoppingListSortField.Title, SortDirection.Ascending);

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(readContext);
		var (items, _) = await shoppingLists.GetByOwnerAsync(owner, DefaultPaging, sorting);

		items.Select(i => i.Title.Value).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task GetByOwnerAsync_SortByTitleDescending_ReturnsSorted()
	{
		await fixture.SeedShoppingListsAsync(
			ShoppingListFactory.WeeklyGroceries(owner.Value),
			ShoppingListFactory.BakingIngredients(owner.Value),
			ShoppingListFactory.PartySupplies(owner.Value));
		var sorting = new SortingOptions<ShoppingListSortField>(ShoppingListSortField.Title, SortDirection.Descending);

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(readContext);
		var (items, _) = await shoppingLists.GetByOwnerAsync(owner, DefaultPaging, sorting);

		items.Select(i => i.Title.Value).Should().BeInDescendingOrder();
	}

	[Fact]
	public async Task GetByOwnerAsync_SortByDateAscending_ReturnsOldestFirst()
	{
		await fixture.SeedShoppingListsAsync(
			ShoppingListFactory.WeeklyGroceries(owner.Value),
			ShoppingListFactory.BakingIngredients(owner.Value),
			ShoppingListFactory.PartySupplies(owner.Value));
		var sorting = new SortingOptions<ShoppingListSortField>(ShoppingListSortField.Date, SortDirection.Ascending);

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(readContext);
		var (items, _) = await shoppingLists.GetByOwnerAsync(owner, DefaultPaging, sorting);

		items.Select(i => i.CreatedAt).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task GetByOwnerAsync_OwnerWithNoLists_ReturnsEmptyResult()
	{
		var loneOwner = OwnerIdentifier.From($"empty-{Guid.NewGuid():N}");

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(readContext);
		var (items, totalCount) = await shoppingLists.GetByOwnerAsync(loneOwner, DefaultPaging, DefaultSorting);

		items.Should().BeEmpty();
		totalCount.Value.Should().Be(0);
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsDetachedEntity()
	{
		var list = ShoppingListFactory.WeeklyGroceries(owner.Value);
		await fixture.SeedShoppingListsAsync(list);

		await using var readContext = await fixture.CreateShoppingDbContextAsync();
		var shoppingLists = new ShoppingListRepository(readContext);
		var loaded = await shoppingLists.GetByIdAsync(list.Id);

		readContext.Entry(loaded).State.Should().Be(EntityState.Detached);
	}
}
