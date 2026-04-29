using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.EventStore;

[Collection(AspireCollection.Name)]
public class ShoppingListBackfillServiceTests(AspireFixture fixture) : IAsyncLifetime
{
	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"backfill-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		await fixture.ResetShoppingListEventStoreAsync(owner);
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var summaries = await ctx.Set<ShoppingListSummaryReadItem>().Where(s => s.OwnerId == owner.Value).ToListAsync();
		var items = await ctx.Set<ShoppingListItemReadItem>().Where(i => i.OwnerId == owner.Value).ToListAsync();
		var lists = await ctx.ShoppingLists.Where(l => l.Owner == owner).ToListAsync();
		ctx.RemoveRange(summaries);
		ctx.RemoveRange(items);
		ctx.RemoveRange(lists);
		await ctx.SaveChangesAsync();
	}

	[Fact]
	public async Task BackfillAsync_LegacyListWithoutEvents_EmitsCreatedAndItemAddedEvents()
	{
		var listId = await SeedLegacyOnlyAsync("Pantry", checkedItem: false);

		var count = await RunBackfillAsync();

		count.Should().Be(1);
		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var events = await verify.Set<ShoppingListStoredEvent>()
			.Where(e => e.AggregateId == listId.Value)
			.OrderBy(e => e.Version)
			.ToListAsync();
		events.Should().HaveCount(2);
		events[0].EventType.Should().Be(nameof(SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events.ShoppingListCreated));
		events[0].Version.Should().Be(1);
		events[1].EventType.Should().Be(nameof(SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events.ListItemAdded));
		events[1].Version.Should().Be(2);

		var metadata = await verify.Set<ShoppingListAggregateMetadata>()
			.SingleAsync(m => m.AggregateId == listId.Value);
		metadata.OwnerId.Should().Be(owner.Value);
	}

	[Fact]
	public async Task BackfillAsync_LegacyListWithCheckedItem_EmitsListItemCheckedAfterAdd()
	{
		var listId = await SeedLegacyOnlyAsync("Pantry", checkedItem: true);

		await RunBackfillAsync();

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var events = await verify.Set<ShoppingListStoredEvent>()
			.Where(e => e.AggregateId == listId.Value)
			.OrderBy(e => e.Version)
			.ToListAsync();
		events.Should().HaveCount(3);
		events[2].EventType.Should().Be(nameof(SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events.ListItemChecked));
	}

	[Fact]
	public async Task BackfillAsync_RunTwice_SkipsAlreadyBackfilledLists()
	{
		await SeedLegacyOnlyAsync("Pantry", checkedItem: false);

		var firstRun = await RunBackfillAsync();
		var secondRun = await RunBackfillAsync();

		firstRun.Should().Be(1);
		secondRun.Should().Be(0);
	}

	[Fact]
	public async Task BackfillAsync_FollowedByProjectionRebuild_PopulatesProjectionTables()
	{
		var listId = await SeedLegacyOnlyAsync("Pantry", checkedItem: true);

		await RunBackfillAsync();

		// Run the projection rebuilder against the backfilled events.
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var projection = new ShoppingListProjection(ctx);
			var rebuilder = new ShoppingListProjectionRebuilder(
				ctx,
				projection,
				NullLogger<ShoppingListProjectionRebuilder>.Instance);
			await rebuilder.RebuildAsync();
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var summary = await verify.Set<ShoppingListSummaryReadItem>().SingleAsync(s => s.Id == listId.Value);
		summary.Title.Should().Be("Pantry");
		summary.ItemCount.Should().Be(1);
		var item = await verify.Set<ShoppingListItemReadItem>().SingleAsync(i => i.ListId == listId.Value);
		item.IsChecked.Should().BeTrue();
	}

	private async Task<int> RunBackfillAsync()
	{
		await using var ctx = await fixture.CreateShoppingDbContextAsync();
		var service = new ShoppingListBackfillService(ctx, NullLogger<ShoppingListBackfillService>.Instance);
		return await service.BackfillAsync();
	}

	/// <summary>
	/// Inserts a list directly into the legacy ShoppingLists table, bypassing
	/// the event-sourced path so the test mirrors a row that pre-dates Phase 2.
	/// EF tracks aggregate state via the ChangeTracker without involving the
	/// UoW's event-publishing logic.
	/// </summary>
	private async Task<ShoppingListIdentifier> SeedLegacyOnlyAsync(string title, bool checkedItem)
	{
		await using var ctx = await fixture.CreateShoppingDbContextAsync();

		// We can't construct the aggregate via Create() without raising events,
		// so build it through reflection-friendly state directly. Easiest path:
		// instantiate via Create() then DETACH from any change tracker and
		// re-add as Added without keeping uncommitted events on the heap.
		var item = ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram));
		var list = ShoppingList.Create(
			ShoppingListTitle.From(title),
			owner,
			[item]);

		if (checkedItem)
		{
			list.CheckOffItem(item.Id);
		}

		// Drop any uncommitted events so the dual-write path is bypassed —
		// the row only ends up in the legacy table.
		list.MarkCommitted();
		ctx.ShoppingLists.Add(list);
		await ctx.SaveChangesAsync();
		return list.Identifier;
	}
}
