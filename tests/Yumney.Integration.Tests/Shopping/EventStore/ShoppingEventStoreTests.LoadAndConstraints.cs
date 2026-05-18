using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.EventStore;

#pragma warning disable SA1601
public partial class ShoppingEventStoreTests
#pragma warning restore SA1601
{
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
	public async Task FindAsync_OtherOwnerHasEvents_ReturnsNullForThisOwner()
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
			var loaded = await CreateStore(freshContext).FindAsync(owner);

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
}
