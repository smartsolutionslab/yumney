using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Events;
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
	public async Task FindAsync_NoEventsForIdentifier_ReturnsNull()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);

		var loaded = await store.FindAsync(ShoppingListIdentifier.New());

		loaded.Should().BeNull();
	}

	[Fact]
	public async Task SaveAsync_NewList_PersistsEventsAndMetadata()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);
		var list = CreateList();

		await store.SaveAsync(list);

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var stored = await verify.Set<ShoppingListStoredEvent>()
			.Where(stored => stored.AggregateId == list.Identifier.Value)
			.OrderBy(stored => stored.Version)
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
	public async Task SaveAsync_ClearsUncommittedEventsAfterPersist()
	{
		var list = CreateList();

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			await CreateStore(context).SaveAsync(list);
		}

		list.UncommittedEvents.Should().BeEmpty();
		list.CheckOffItem(list.Items[0].Id);

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			await CreateStore(context).SaveAsync(list);
		}

		await using var verify = await fixture.CreateShoppingDbContextAsync();
		var stored = await verify.Set<ShoppingListStoredEvent>()
			.Where(stored => stored.AggregateId == list.Identifier.Value)
			.OrderBy(stored => stored.Version)
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
			await CreateStore(context).SaveAsync(original);
		}

		await using var loadContext = await fixture.CreateShoppingDbContextAsync();
		var loaded = await CreateStore(loadContext).LoadAsync(original.Identifier);

		loaded.Should().NotBeNull();
		loaded!.Identifier.Should().Be(original.Identifier);
		loaded.Title.Should().Be(original.Title);
		loaded.Owner.Should().Be(original.Owner);
		loaded.Items.Should().HaveCount(1);
		loaded.Items[0].IsChecked.Should().BeTrue();
		loaded.Version.Should().Be(original.Version);
	}

	[Fact]
	public async Task SaveAsync_ConflictingVersion_ThrowsConcurrencyConflictException()
	{
		var list = CreateList();

		await using (var context = await fixture.CreateShoppingDbContextAsync())
		{
			await CreateStore(context).SaveAsync(list);
		}

		// Simulate a competing write at the next version slot.
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

		var act = () => store2.SaveAsync(list);

		await act.Should().ThrowAsync<ConcurrencyConflictException>();
	}

	private static EfCoreShoppingListEventStore CreateStore(ShoppingDbContext context) =>
		new(context, Substitute.For<IEventBus>(), NullLogger<EfCoreShoppingListEventStore>.Instance);

	private ShoppingList CreateList()
	{
		var item = ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram));
		return ShoppingList.Create(
			ShoppingListTitle.From("Weekly Groceries"),
			owner,
			[item]);
	}
}
