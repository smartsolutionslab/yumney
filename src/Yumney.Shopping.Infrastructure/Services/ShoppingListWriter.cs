using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Services;

public sealed class ShoppingListWriter(IShoppingEventStore eventStore) : IShoppingListWriter
{
	public async Task AddItemsAsync(
		string ownerId,
		IReadOnlyList<ShoppingItemRequest> items,
		CancellationToken cancellationToken = default)
	{
		var owner = OwnerIdentifier.From(ownerId);
		var ledger = await eventStore.LoadAsync(owner, cancellationToken) ?? ShoppingLedger.Create(owner);

		foreach (var (itemName, quantity, unit, source) in items)
		{
			ledger.AddItem(
				ItemName.From(itemName),
				Quantity.Of(
					Amount.From(quantity),
					Unit.FromNullable(unit)),
				ItemSource.From(source));
		}

		await eventStore.SaveAsync(ledger, cancellationToken);
	}
}
