using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;

/// <summary>
/// Cross-module reaction to <see cref="MealConfirmedIntegrationEvent"/>.
/// For each ingredient carried in the event, marks the corresponding quantity
/// as consumed on the owner's shopping ledger. Items that are not on the
/// ledger are tolerated (the ledger clamps negative values).
/// </summary>
public sealed class MealConfirmedHandler(IShoppingEventStore eventStore)
	: IIntegrationEventHandler<MealConfirmedIntegrationEvent>
{
	/// <inheritdoc />
	public async Task HandleAsync(MealConfirmedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		if (@event.Ingredients.Count == 0) return;

		var owner = OwnerIdentifier.From(@event.OwnerId);
		var ledger = await eventStore.FindAsync(owner, cancellationToken);
		if (ledger is null) return;

		var source = ItemSource.From("meal-plan");
		foreach (var ingredient in @event.Ingredients)
		{
			var itemName = ItemName.From(ingredient.Name);
			var quantity = Quantity.Of(Amount.From(ingredient.Quantity), Unit.FromNullable(ingredient.Unit));
			ledger.MarkConsumed(itemName, quantity, source);
		}

		await eventStore.SaveAsync(ledger, cancellationToken);
	}
}
