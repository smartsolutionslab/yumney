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
        var ledger = await eventStore.LoadAsync(ownerId, cancellationToken)
                     ?? ShoppingLedger.Create(ownerId);

        foreach (var item in items)
        {
            ledger.AddItem(ItemName.From(item.ItemName), Amount.From(item.Quantity), Unit.FromNullable(item.Unit), item.Source);
        }

        await eventStore.SaveAsync(ledger, cancellationToken);
    }
}
