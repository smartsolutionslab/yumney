using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.ReadModel;

[Collection(AspireCollection.Name)]
public class ShoppingListProjectionRebuilderTests(AspireFixture fixture) : IAsyncLifetime
{
	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"rebuild-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await fixture.ResetShoppingListEventStoreAsync(owner);
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var summaries = await ctx.Set<ShoppingListSummaryReadItem>().Where(summary => summary.OwnerId == owner.Value).ToListAsync();
		var items = await ctx.Set<ShoppingListItemReadItem>().Where(item => item.OwnerId == owner.Value).ToListAsync();
		ctx.RemoveRange(summaries);
		ctx.RemoveRange(items);
		await ctx.SaveChangesAsync();
	}

	[Fact]
	public async Task RebuildAsync_WithSeededEvents_RestoresProjectionTables()
	{
		var listId = await SeedListAsync("Weekly Groceries", "Flour", "Sugar");
		await CheckOffFirstItemAsync(listId);
		await TruncateProjectionsForOwnerAsync();

		var replayed = await RunRebuildAsync();

		replayed.Should().BeGreaterThan(0);
		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var summary = await verify.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == listId.Value);
		summary.Title.Should().Be("Weekly Groceries");
		summary.ItemCount.Should().Be(2);
		var items = await verify.Set<ShoppingListItemReadItem>().Where(item => item.ListId == listId.Value).ToListAsync();
		items.Should().HaveCount(2);
		items.Count(item => item.IsChecked).Should().Be(1);
	}

	[Fact]
	public async Task RebuildAsync_RunTwice_YieldsSameProjectionState()
	{
		var listId = await SeedListAsync("Pantry", "Salt", "Pepper", "Oil");
		await TruncateProjectionsForOwnerAsync();

		await RunRebuildAsync();
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var firstSummary = await ctx.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == listId.Value);
			firstSummary.ItemCount.Should().Be(3);
		}

		await RunRebuildAsync();
		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var summary = await verify.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == listId.Value);
		var items = await verify.Set<ShoppingListItemReadItem>().Where(item => item.ListId == listId.Value).CountAsync();
		summary.ItemCount.Should().Be(3);
		items.Should().Be(3);
	}

	private async Task<int> RunRebuildAsync()
	{
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var projection = new ShoppingListProjection(ctx);
		var rebuilder = new ShoppingListProjectionRebuilder(
			ctx,
			projection,
			NullLogger<ShoppingListProjectionRebuilder>.Instance);
		return await rebuilder.RebuildAsync();
	}

	private async Task TruncateProjectionsForOwnerAsync()
	{
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var summaries = await ctx.Set<ShoppingListSummaryReadItem>().Where(summary => summary.OwnerId == owner.Value).ToListAsync();
		var items = await ctx.Set<ShoppingListItemReadItem>().Where(item => item.OwnerId == owner.Value).ToListAsync();
		ctx.RemoveRange(summaries);
		ctx.RemoveRange(items);
		await ctx.SaveChangesAsync();
	}

	private async Task<ShoppingListIdentifier> SeedListAsync(string title, params string[] itemNames)
	{
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var bus = Substitute.For<IEventBus>();
		var store = new EfCoreShoppingListEventStore(ctx, bus, NullLogger<EfCoreShoppingListEventStore>.Instance);

		var items = itemNames
			.Select(name => ShoppingListItem.Create(ItemName.From(name), Quantity.Of(Amount.From(1), Unit.Gram)))
			.ToList();
		var list = ShoppingList.Create(ShoppingListTitle.From(title), owner, items);
		await store.SaveAsync(list);
		return list.Identifier;
	}

	private async Task CheckOffFirstItemAsync(ShoppingListIdentifier listId)
	{
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var bus = Substitute.For<IEventBus>();
		var store = new EfCoreShoppingListEventStore(ctx, bus, NullLogger<EfCoreShoppingListEventStore>.Instance);

		var list = await store.LoadAsync(listId) ?? throw new InvalidOperationException("seeded list missing");
		list.CheckOffItem(list.Items[0].Id);
		await store.SaveAsync(list);
	}
}
