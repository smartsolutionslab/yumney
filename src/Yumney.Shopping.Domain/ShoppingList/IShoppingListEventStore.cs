using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

/// <summary>
/// Append-only event store for the <see cref="ShoppingList"/> aggregate. Events are
/// staged on the underlying <see cref="Microsoft.EntityFrameworkCore.DbContext"/> by
/// <see cref="AppendAsync"/> and flushed by the <see cref="IShoppingUnitOfWork"/> in
/// the same transaction as any relational state writes.
/// </summary>
public interface IShoppingListEventStore
{
	Task<ShoppingList?> LoadAsync(ShoppingListIdentifier identifier, CancellationToken cancellationToken = default);

	Task AppendAsync(ShoppingList list, CancellationToken cancellationToken = default);
}
