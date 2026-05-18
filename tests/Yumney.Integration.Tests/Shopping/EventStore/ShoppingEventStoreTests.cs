using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using Wolverine.EntityFrameworkCore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.EventStore;

[Collection(AspireCollection.Name)]
#pragma warning disable SA1601
public partial class ShoppingEventStoreTests(AspireFixture fixture) : IAsyncLifetime
#pragma warning restore SA1601
{
	private static readonly ItemSource Source = ItemSource.From("manual");

	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"evtstore-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => fixture.ResetShoppingEventStoreAsync(owner);

	[Fact]
	public async Task FindAsync_NoEventsForOwner_ReturnsNull()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);

		var ledger = await store.FindAsync(owner);

		ledger.Should().BeNull();
	}

	[Fact]
	public async Task SaveAsync_NewLedgerWithItems_PersistsEventsAndMetadata()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(ItemName.From("Milk"), Quantity.Of(Amount.From(2), Unit.From("l")), Source);

		await store.SaveAsync(ledger);

		await using var verifyContext = await fixture.CreateShoppingDbContextAsync();
		var stored = await verifyContext.Set<StoredEvent>()
			.Where(stored => stored.AggregateId == ledger.Identifier).ToListAsync();
		var metadata = await verifyContext.Set<AggregateMetadata>()
			.SingleAsync(m => m.AggregateId == ledger.Identifier);
		stored.Should().HaveCount(1);
		stored[0].EventType.Should().Be(nameof(ShoppingItemAdded));
		stored[0].Version.Should().Be(1);
		metadata.OwnerId.Should().Be(owner.Value);
	}

	[Fact]
	public async Task SaveAsync_NoUncommittedEvents_WritesNothing()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);
		var ledger = ShoppingLedger.Create(owner);

		await store.SaveAsync(ledger);

		await using var verifyContext = await fixture.CreateShoppingDbContextAsync();
		var hasEvents = await verifyContext.Set<StoredEvent>()
			.AnyAsync(e => e.AggregateId == ledger.Identifier);
		var hasMetadata = await verifyContext.Set<AggregateMetadata>()
			.AnyAsync(m => m.OwnerId == owner.Value);
		hasEvents.Should().BeFalse();
		hasMetadata.Should().BeFalse();
	}

	[Fact]
	public async Task SaveAsync_ClearsUncommittedEventsAfterWrite()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")), Source);

		await store.SaveAsync(ledger);

		ledger.UncommittedEvents.Should().BeEmpty();
	}

	[Fact]
	public async Task SaveAsync_CancelledToken_ThrowsOperationCanceled()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")), Source);

		var act = () => store.SaveAsync(ledger, cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private static ShoppingEventStore CreateStore(
		ShoppingDbContext context,
		IEventBus? bus = null,
		IDbContextOutbox<ShoppingDbContext>? outbox = null)
	{
		return new ShoppingEventStore(
			context,
			bus ?? Substitute.For<IEventBus>(),
			outbox ?? Substitute.For<IDbContextOutbox<ShoppingDbContext>>(),
			NullLogger<ShoppingEventStore>.Instance);
	}
}
