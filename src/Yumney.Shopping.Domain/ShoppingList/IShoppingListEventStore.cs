namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

/// <summary>
/// Append-only event store for the <see cref="ShoppingList"/> aggregate. Mirrors
/// <c>IMealPlanEventStore</c>: <see cref="SaveAsync"/> appends + persists +
/// publishes integration events in one call, so command handlers operate on
/// the event store directly without a unit-of-work or repository layer.
/// </summary>
public interface IShoppingListEventStore
{
	Task<ShoppingList?> LoadAsync(ShoppingListIdentifier identifier, CancellationToken cancellationToken = default);

	/// <summary>
	/// Appends the aggregate's uncommitted events, persists, marks the aggregate
	/// committed, and publishes integration events. Throws
	/// <see cref="ConcurrencyConflictException"/> if a competing writer staged
	/// events with conflicting versions.
	/// </summary>
	/// <param name="list">The aggregate to persist.</param>
	/// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
	/// <returns>A task representing the asynchronous save.</returns>
	Task SaveAsync(ShoppingList list, CancellationToken cancellationToken = default);
}
