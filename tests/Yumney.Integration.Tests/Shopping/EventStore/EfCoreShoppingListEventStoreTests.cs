using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.EventStore;

[Collection(AspireCollection.Name)]
public class EfCoreShoppingListEventStoreTests(AspireFixture fixture) : IAsyncLifetime
{
	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"listevtstore-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => fixture.ResetShoppingListEventStoreAsync(owner);

	[Fact]
	public async Task LoadAsync_NoEventsForIdentifier_ReturnsNull()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);

		var loaded = await store.LoadAsync(ShoppingListIdentifier.New());

		loaded.Should().BeNull();
	}

	[Fact]
	public async Task AppendAsync_NewList_StagesEventsAndMetadata()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);
		var list = CreateList();

		await store.AppendAsync(list);
		await context.SaveChangesAsync();

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var stored = await verify.Set<ShoppingListStoredEvent>()
			.Where(e => e.AggregateId == list.Identifier.Value)
			.OrderBy(e => e.Version)
			.ToListAsync();
		var metadata = await verify.Set<ShoppingListAggregateMetadata>()
			.SingleAsync(m => m.AggregateId == list.Identifier.Value);

		stored.Should().HaveCount(2);
		stored[0].EventType.Should().Be(nameof(ShoppingListCreated));
		stored[0].Version.Should().Be(1);
		stored[1].EventType.Should().Be(nameof(ListItemAdded));
		stored[1].Version.Should().Be(2);
		metadata.OwnerId.Should().Be(owner.Value);
	}

	[Fact]
	public async Task AppendAsync_OnlyAfterMarkCommitted_DoesNotDoubleStage()
	{
		var list = CreateList();

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = CreateStore(context);
			await store.AppendAsync(list);
			await context.SaveChangesAsync();
			list.MarkCommitted();
		}

		list.CheckOffItem(list.Items[0].Id);

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = CreateStore(context);
			await store.AppendAsync(list);
			await context.SaveChangesAsync();
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var stored = await verify.Set<ShoppingListStoredEvent>()
			.Where(e => e.AggregateId == list.Identifier.Value)
			.OrderBy(e => e.Version)
			.ToListAsync();

		stored.Should().HaveCount(3);
		stored[2].EventType.Should().Be(nameof(ListItemChecked));
		stored[2].Version.Should().Be(3);
	}

	[Fact]
	public async Task LoadAsync_ReplaysEventsToSameState()
	{
		var original = CreateList();
		original.CheckOffItem(original.Items[0].Id);

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = CreateStore(context);
			await store.AppendAsync(original);
			await context.SaveChangesAsync();
		}

		await using var loadContext = await fixture.CreateShoppingDbContextAsync();
		var loadStore = CreateStore(loadContext);
		var loaded = await loadStore.LoadAsync(original.Identifier);

		loaded.Should().NotBeNull();
		loaded!.Identifier.Should().Be(original.Identifier);
		loaded.Title.Should().Be(original.Title);
		loaded.Owner.Should().Be(original.Owner);
		loaded.Items.Should().HaveCount(1);
		loaded.Items[0].IsChecked.Should().BeTrue();
		loaded.Version.Should().Be(original.Version);
	}

	[Fact]
	public async Task AppendAsync_ConflictingVersion_ThrowsOnSave()
	{
		var list = CreateList();

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			var store = CreateStore(context);
			await store.AppendAsync(list);
			await context.SaveChangesAsync();
			list.MarkCommitted();
		}

		// Simulate a competing write at version 2 by manually inserting a row with the
		// same (AggregateId, Version) as the next staged event would use.
		await using (var conflictContext = await fixture.CreateShoppingDbContextAsync())
		{
			conflictContext.Set<ShoppingListStoredEvent>().Add(new ShoppingListStoredEvent
			{
				Id = Guid.CreateVersion7(),
				AggregateId = list.Identifier.Value,
				EventType = nameof(ListItemChecked),
				EventData = "{}",
				Version = list.Version + 1,
				OccurredAt = DateTime.UtcNow,
			});
			await conflictContext.SaveChangesAsync();
		}

		list.CheckOffItem(list.Items[0].Id);

		await using var context2 = await fixture.CreateShoppingDbContextAsync();
		var store2 = CreateStore(context2);
		await store2.AppendAsync(list);

		var act = () => context2.SaveChangesAsync();
		var ex = await act.Should().ThrowAsync<DbUpdateException>();
		ex.Which.IsUniqueViolation().Should().BeTrue();
	}

	private static EfCoreShoppingListEventStore CreateStore(ShoppingDbContext context) =>
		new(context, NullLogger<EfCoreShoppingListEventStore>.Instance);

	private ShoppingList CreateList()
	{
		var item = ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram));
		return ShoppingList.Create(
			ShoppingListTitle.From("Weekly Groceries"),
			owner,
			[item]);
	}
}
