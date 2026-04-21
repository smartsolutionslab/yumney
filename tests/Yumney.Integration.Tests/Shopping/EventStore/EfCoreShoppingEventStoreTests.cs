using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.EventStore;

[Collection(AspireCollection.Name)]
public class EfCoreShoppingEventStoreTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly ItemSource Source = ItemSource.From("manual");

	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"evtstore-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => fixture.ResetShoppingEventStoreAsync(owner);

	[Fact]
	public async Task LoadAsync_NoEventsForOwner_ReturnsNull()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var store = CreateStore(context);

		var ledger = await store.LoadAsync(owner);

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
			.Where(e => e.AggregateId == ledger.Identifier).ToListAsync();
		var metadata = await verifyContext.Set<AggregateMetadata>()
			.SingleAsync(m => m.AggregateId == ledger.Identifier);
		stored.Should().HaveCount(1);
		stored[0].EventType.Should().Be(nameof(ShoppingItemAdded));
		stored[0].Version.Should().Be(1);
		metadata.OwnerId.Should().Be(owner.Value);
	}

	[Fact]
	public async Task SaveAsync_ExistingLedger_AssignsContiguousVersions()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");

		ShoppingLedger ledger;
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var store = CreateStore(ctx);
			ledger = ShoppingLedger.Create(owner)
				.AddItem(milk, Quantity.Of(Amount.From(1), litre), Source);
			await store.SaveAsync(ledger);
		}

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			ledger.AddItem(milk, Quantity.Of(Amount.From(1), litre), Source);
			await CreateStore(ctx).SaveAsync(ledger);
		}

		await using var verifyContext = await fixture.CreateShoppingDbContextAsync();
		var versions = await verifyContext.Set<StoredEvent>()
			.Where(e => e.AggregateId == ledger.Identifier)
			.OrderBy(e => e.Version).Select(e => e.Version).ToListAsync();
		versions.Should().Equal(1, 2);
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
	public async Task SaveAsync_PublishesIntegrationEventsForKnownEvents()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var bus = Substitute.For<IEventBus>();
		var store = CreateStore(context, bus);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")), Source);

		await store.SaveAsync(ledger);

		await bus.Received(1).PublishAsync(
			Arg.Is<ShoppingItemAddedIntegrationEvent>(e => e.OwnerId == owner.Value),
			Arg.Any<CancellationToken>());
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
	public async Task LoadAsync_AfterSaveRoundtrip_RehydratesLedgerState()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var ledger = ShoppingLedger.Create(owner)
				.AddItem(milk, Quantity.Of(Amount.From(2), litre), Source)
				.MarkBought(milk, Quantity.Of(Amount.From(1), litre));
			await CreateStore(ctx).SaveAsync(ledger);
		}

		await using var freshContext = await fixture.CreateShoppingDbContextAsync();
		var loaded = await CreateStore(freshContext).LoadAsync(owner);

		loaded.Should().NotBeNull();
		loaded!.OwnerId.Should().Be(owner);
		loaded.Version.Value.Should().Be(2);
		var state = loaded.Items.Values.Single();
		state.OnList.Value.Should().Be(2m);
		state.Bought.Value.Should().Be(1m);
	}

	[Fact]
	public async Task SaveAsync_WritesSnapshotWhenVersionReachesInterval()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");
		var ledger = ShoppingLedger.Create(owner);
		for (var i = 0; i < 50; i++)
		{
			ledger.AddItem(milk, Quantity.Of(Amount.From(1), litre), Source);
		}

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await CreateStore(ctx).SaveAsync(ledger);
		}

		await using var verifyContext = await fixture.CreateShoppingDbContextAsync();
		var snapshot = await verifyContext.Set<StoredSnapshot>()
			.SingleAsync(s => s.AggregateId == ledger.Identifier);
		snapshot.Version.Should().Be(50);
		ledger.Version.Value.Should().Be(50);
	}

	[Fact]
	public async Task LoadAsync_WithSnapshot_UsesSnapshotAsBaseline()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");
		var ledger = ShoppingLedger.Create(owner);
		for (var i = 0; i < 50; i++)
		{
			ledger.AddItem(milk, Quantity.Of(Amount.From(1), litre), Source);
		}

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			await CreateStore(ctx).SaveAsync(ledger);
		}

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			ledger.AddItem(milk, Quantity.Of(Amount.From(3), litre), Source);
			await CreateStore(ctx).SaveAsync(ledger);
		}

		await using var freshContext = await fixture.CreateShoppingDbContextAsync();
		var loaded = await CreateStore(freshContext).LoadAsync(owner);

		loaded.Should().NotBeNull();
		loaded!.Version.Value.Should().Be(51);
		loaded.Items.Values.Single().OnList.Value.Should().Be(53m);
	}

	private static EfCoreShoppingEventStore CreateStore(ShoppingDbContext context, IEventBus? bus = null)
	{
		return new EfCoreShoppingEventStore(
			context,
			bus ?? Substitute.For<IEventBus>(),
			NullLogger<EfCoreShoppingEventStore>.Instance);
	}
}
