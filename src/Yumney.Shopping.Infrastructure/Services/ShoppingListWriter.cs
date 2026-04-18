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
		var ledger = await eventStore.LoadAsync(owner, cancellationToken)
					 ?? ShoppingLedger.Create(owner);

		foreach (var item in items)
		{
			ledger.AddItem(
				ItemName.From(item.ItemName),
				Quantity.Of(
					Amount.From(item.Quantity),
					Unit.FromNullable(item.Unit)),
				ItemSource.From(item.Source));
		}

		await eventStore.SaveAsync(ledger, cancellationToken);
	}
}
