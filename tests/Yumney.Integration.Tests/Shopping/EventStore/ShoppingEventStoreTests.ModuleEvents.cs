using NSubstitute;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;
using Wolverine.EntityFrameworkCore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.EventStore;

#pragma warning disable SA1601
public partial class ShoppingEventStoreTests
#pragma warning restore SA1601
{
	[Fact]
	public async Task SaveAsync_PublishesModuleEventsForKnownEvents()
	{
		await using var context = await fixture.CreateShoppingDbContextAsync();
		var outbox = Substitute.For<IDbContextOutbox<ShoppingDbContext>>();
		var store = CreateStore(context, outbox: outbox);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")), Source);

		await store.SaveAsync(ledger);

		await outbox.Received(1).PublishAsync(
			Arg.Is<ShoppingItemAddedModuleEvent>(moduleEvent => moduleEvent.OwnerId == owner.Value));
	}

	[Fact]
	public async Task SaveAsync_MultipleEventTypes_PublishesModuleEventForEach()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");
		var oneLitre = Quantity.Of(Amount.From(1), litre);

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var outbox = Substitute.For<IDbContextOutbox<ShoppingDbContext>>();
		var store = CreateStore(context, outbox: outbox);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(milk, oneLitre, Source)
			.MarkBought(milk, oneLitre)
			.MarkConsumed(milk, oneLitre, Source)
			.AdjustQuantity(milk, Quantity.Of(Amount.From(3), litre))
			.RemoveItem(milk, oneLitre);

		await store.SaveAsync(ledger);

		await outbox.Received(1).PublishAsync(Arg.Any<ShoppingItemAddedModuleEvent>());
		await outbox.Received(1).PublishAsync(Arg.Any<ShoppingItemBoughtModuleEvent>());
		await outbox.Received(1).PublishAsync(Arg.Any<ShoppingItemConsumedModuleEvent>());
		await outbox.Received(1).PublishAsync(Arg.Any<ShoppingItemQuantityAdjustedModuleEvent>());
		await outbox.Received(1).PublishAsync(Arg.Any<ShoppingItemRemovedModuleEvent>());
	}

	[Fact]
	public async Task SaveAsync_UndoBought_PublishesUndoBoughtModuleEvent()
	{
		var milk = ItemName.From("Milk");
		var litre = Unit.From("l");
		var oneLitre = Quantity.Of(Amount.From(1), litre);

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var outbox = Substitute.For<IDbContextOutbox<ShoppingDbContext>>();
		var store = CreateStore(context, outbox: outbox);
		var ledger = ShoppingLedger.Create(owner)
			.AddItem(milk, oneLitre, Source)
			.MarkBought(milk, oneLitre)
			.UndoBought(milk, oneLitre);

		await store.SaveAsync(ledger);

		await outbox.Received(1).PublishAsync(
			Arg.Is<ShoppingItemUndoBoughtModuleEvent>(moduleEvent => moduleEvent.OwnerId == owner.Value));
	}

	[Fact]
	public async Task SaveAsync_MarkedAsFrozen_PublishesMarkedAsFrozenModuleEvent()
	{
		var chicken = ItemName.From("Chicken");
		var grams = Unit.From("g");

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var outbox = Substitute.For<IDbContextOutbox<ShoppingDbContext>>();
		var store = CreateStore(context, outbox: outbox);
		var ledger = ShoppingLedger.Create(owner)
			.AddAsAtHome(chicken, Quantity.Of(Amount.From(500), grams))
			.MarkAsFrozen(chicken, grams);

		await store.SaveAsync(ledger);

		await outbox.Received(1).PublishAsync(
			Arg.Is<ShoppingItemMarkedAsFrozenModuleEvent>(moduleEvent =>
				moduleEvent.OwnerId == owner.Value && moduleEvent.Inner.ItemName.Value == "Chicken"));
	}

	[Fact]
	public async Task SaveAsync_AddedAsAtHome_PublishesAddedAsAtHomeModuleEvent()
	{
		var butter = ItemName.From("Butter");
		var grams = Unit.From("g");

		await using var context = await fixture.CreateShoppingDbContextAsync();
		var outbox = Substitute.For<IDbContextOutbox<ShoppingDbContext>>();
		var store = CreateStore(context, outbox: outbox);
		var ledger = ShoppingLedger.Create(owner)
			.AddAsAtHome(butter, Quantity.Of(Amount.From(250), grams));

		await store.SaveAsync(ledger);

		await outbox.Received(1).PublishAsync(
			Arg.Is<ShoppingItemAddedAsAtHomeModuleEvent>(moduleEvent =>
				moduleEvent.OwnerId == owner.Value && moduleEvent.Inner.ItemName.Value == "Butter"));
	}
}
