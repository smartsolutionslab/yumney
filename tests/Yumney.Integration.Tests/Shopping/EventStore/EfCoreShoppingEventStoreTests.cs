using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
			.Where(stored => stored.AggregateId == ledger.Identifier).ToListAsync();
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
			.Where(stored => stored.AggregateId == ledger.Identifier)
			.OrderBy(stored => stored.Version).Select(stored => stored.Version).ToListAsync();
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
	public async Task SaveAsync_PublishesModuleEventsForKnownEvents()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var bus = Substitute.For<IEventBus>();
		var store = CreateStore(context, bus);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")), Source);

		await store.SaveAsync(ledger);

		await bus.Received(1).PublishAsync(
			Arg.Is<ShoppingItemAddedModuleEvent>(e => e.OwnerId == owner.Value),
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
	public async Task SaveAsync_SecondLedgerForSameOwner_ViolatesUniqueOwnerIdConstraint()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			var first = ShoppingLedger.Create(owner)
				.AddItem(milk, Quantity.Of(Amount.From(1), litre), Source);
			await CreateStore(ctx).SaveAsync(first);
		}

		await using var ctx2 = await fixture.CreateShoppingDbContextAsync();
		var second = ShoppingLedger.Create(owner)
			.AddItem(milk, Quantity.Of(Amount.From(1), litre), Source);
		var act = () => CreateStore(ctx2).SaveAsync(second);

		await act.Should().ThrowAsync<ConcurrencyConflictException>();
	}

	[Fact]
	public async Task SaveAsync_DuplicateAggregateVersion_ViolatesUniqueVersionConstraint()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");
		ShoppingLedger ledger;
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			ledger = ShoppingLedger.Create(owner)
				.AddItem(milk, Quantity.Of(Amount.From(1), litre), Source);
			await CreateStore(ctx).SaveAsync(ledger);
		}

		await using var verifyContext = await fixture.CreateShoppingDbContextAsync();
		verifyContext.Set<StoredEvent>().Add(new StoredEvent
		{
			Id = Guid.CreateVersion7(),
			AggregateId = ledger.Identifier,
			EventType = nameof(ShoppingItemAdded),
			EventData = "{}",
			Version = 1,
			OccurredAt = DateTime.UtcNow,
		});
		var act = () => verifyContext.SaveChangesAsync();

		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact]
	public async Task LoadAsync_OtherOwnerHasEvents_ReturnsNullForThisOwner()
	{
		var otherOwner = OwnerIdentifier.From($"evtstore-other-{Guid.NewGuid():N}");
		try
		{
			await using (var ctx = await fixture.CreateShoppingDbContextAsync())
			{
				var ledger = ShoppingLedger.Create(otherOwner)
					.AddItem(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")), Source);
				await CreateStore(ctx).SaveAsync(ledger);
			}

			await using var freshContext = await fixture.CreateShoppingDbContextAsync();
			var loaded = await CreateStore(freshContext).LoadAsync(owner);

			loaded.Should().BeNull();
		}
		finally
		{
			await fixture.ResetShoppingEventStoreAsync(otherOwner);
		}
	}

	[Fact]
	public async Task LoadAsync_UnknownEventTypeInStream_IsSkippedGracefully()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");
		ShoppingLedger ledger;
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			ledger = ShoppingLedger.Create(owner)
				.AddItem(milk, Quantity.Of(Amount.From(2), litre), Source);
			await CreateStore(ctx).SaveAsync(ledger);
		}

		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
			ctx.Set<StoredEvent>().Add(new StoredEvent
			{
				Id = Guid.CreateVersion7(),
				AggregateId = ledger.Identifier,
				EventType = "SomeFutureEventType",
				EventData = "{}",
				Version = 2,
				OccurredAt = DateTime.UtcNow,
			});
			await ctx.SaveChangesAsync();
		}

		await using var freshContext = await fixture.CreateShoppingDbContextAsync();
		var loaded = await CreateStore(freshContext).LoadAsync(owner);

		loaded.Should().NotBeNull();
		loaded!.Items.Values.Single().OnList.Value.Should().Be(2m);
	}

	[Fact]
	public async Task SaveAsync_MultipleEventTypes_PublishesModuleEventForEach()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");
		var oneLitre = Quantity.Of(Amount.From(1), litre);

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var bus = Substitute.For<IEventBus>();
		var store = CreateStore(context, bus);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(milk, oneLitre, Source)
			.MarkBought(milk, oneLitre)
			.MarkConsumed(milk, oneLitre, Source)
			.AdjustQuantity(milk, Quantity.Of(Amount.From(3), litre))
			.RemoveItem(milk, oneLitre);

		await store.SaveAsync(ledger);

		await bus.Received(1).PublishAsync(Arg.Any<ShoppingItemAddedModuleEvent>(), Arg.Any<CancellationToken>());
		await bus.Received(1).PublishAsync(Arg.Any<ShoppingItemBoughtModuleEvent>(), Arg.Any<CancellationToken>());
		await bus.Received(1).PublishAsync(Arg.Any<ShoppingItemConsumedModuleEvent>(), Arg.Any<CancellationToken>());
		await bus.Received(1).PublishAsync(Arg.Any<ShoppingItemQuantityAdjustedModuleEvent>(), Arg.Any<CancellationToken>());
		await bus.Received(1).PublishAsync(Arg.Any<ShoppingItemRemovedModuleEvent>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SaveAsync_UndoBought_PublishesUndoBoughtModuleEvent()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");
		var oneLitre = Quantity.Of(Amount.From(1), litre);

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var bus = Substitute.For<IEventBus>();
		var store = CreateStore(context, bus);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(milk, oneLitre, Source)
			.MarkBought(milk, oneLitre)
			.UndoBought(milk, oneLitre);

		await store.SaveAsync(ledger);

		await bus.Received(1).PublishAsync(
			Arg.Is<ShoppingItemUndoBoughtModuleEvent>(e => e.OwnerId == owner.Value),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SaveAsync_MarkedAsFrozen_PublishesMarkedAsFrozenModuleEvent()
	{
		var chicken = ItemName.From("Chicken");
		var grams = Unit.From("g");

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var bus = Substitute.For<IEventBus>();
		var store = CreateStore(context, bus);
		var ledger = ShoppingLedger.Create(owner)
			.AddAsAtHome(chicken, Quantity.Of(Amount.From(500), grams))
			.MarkAsFrozen(chicken, grams);

		await store.SaveAsync(ledger);

		await bus.Received(1).PublishAsync(
			Arg.Is<ShoppingItemMarkedAsFrozenModuleEvent>(e =>
				e.OwnerId == owner.Value && e.Inner.ItemName.Value == "Chicken"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SaveAsync_AddedAsAtHome_PublishesAddedAsAtHomeModuleEvent()
	{
		var butter = ItemName.From("Butter");
		var grams = Unit.From("g");

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var bus = Substitute.For<IEventBus>();
		var store = CreateStore(context, bus);
		var ledger = ShoppingLedger.Create(owner)
			.AddAsAtHome(butter, Quantity.Of(Amount.From(250), grams));

		await store.SaveAsync(ledger);

		await bus.Received(1).PublishAsync(
			Arg.Is<ShoppingItemAddedAsAtHomeModuleEvent>(e =>
				e.OwnerId == owner.Value && e.Inner.ItemName.Value == "Butter"),
			Arg.Any<CancellationToken>());
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

	[Fact]
	public async Task LoadAsync_LongHistory_ReplaysAllEvents()
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

		ledger.AddItem(milk, Quantity.Of(Amount.From(3), litre), Source);
		await using (var ctx = await fixture.CreateShoppingDbContextAsync())
		{
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
