using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.TestBuilders.Shopping;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.IntegrationEventHandlers;

public class MealConfirmedHandlerTests
{
	private readonly IShoppingEventStore eventStore = Substitute.For<IShoppingEventStore>();
	private readonly MealConfirmedHandler handler;

	public MealConfirmedHandlerTests()
	{
		handler = new MealConfirmedHandler(eventStore);
	}

	[Fact]
	public async Task HandleAsync_NoIngredients_DoesNothing()
	{
		var @event = new MealConfirmedIntegrationEvent("user-123", Guid.NewGuid(), Servings: 2, Ingredients: []);

		await handler.HandleAsync(@event);

		await eventStore.DidNotReceiveWithAnyArgs().FindAsync(default!, default);
		await eventStore.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
	}

	[Fact]
	public async Task HandleAsync_LedgerMissing_DoesNothing()
	{
		var @event = new MealConfirmedIntegrationEvent(
			"user-123",
			Guid.NewGuid(),
			Servings: 2,
			Ingredients: [new MealConfirmedIngredient("Onion", 1m, null)]);
		eventStore.FindAsync(OwnerIdentifier.From("user-123"), Arg.Any<CancellationToken>())
			.Returns((ShoppingLedger?)null);

		await handler.HandleAsync(@event);

		await eventStore.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
	}

	[Fact]
	public async Task HandleAsync_LedgerExists_MarksEachIngredientConsumedAndSaves()
	{
		var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));

		// Seed the ledger so the consumed quantities have something to clamp against.
		ledger.AddItem(ItemName.From("Onion"), Quantity.Of(Amount.From(3m), Unit.FromNullable(null)), ItemSource.From("manual"));
		ledger.AddItem(ItemName.From("Stock"), Quantity.Of(Amount.From(1000m), Unit.From("ml")), ItemSource.From("manual"));
		eventStore.FindAsync(OwnerIdentifier.From("user-123"), Arg.Any<CancellationToken>())
			.Returns(ledger);

		var @event = new MealConfirmedIntegrationEvent(
			"user-123",
			Guid.NewGuid(),
			Servings: 4,
			Ingredients:
			[
				new MealConfirmedIngredient("Onion", 1m, null),
				new MealConfirmedIngredient("Stock", 500m, "ml"),
			]);

		await handler.HandleAsync(@event);

		await eventStore.Received(1).SaveAsync(ledger, Arg.Any<CancellationToken>());
	}
}
