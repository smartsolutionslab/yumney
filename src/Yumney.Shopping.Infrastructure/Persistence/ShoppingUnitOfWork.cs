using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingUnitOfWork(
	ShoppingDbContext context,
	IShoppingListRepository shoppingLists,
	IShoppingListEventStore listEventStore) : IShoppingUnitOfWork
{
	public IShoppingListRepository ShoppingLists => shoppingLists;

	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var trackedLists = context.ChangeTracker.Entries<ShoppingList>()
			.Where(e => e.State != EntityState.Detached)
			.Select(e => e.Entity)
			.Where(l => l.UncommittedEvents.Count > 0)
			.ToList();

		foreach (var list in trackedLists)
		{
			await listEventStore.AppendAsync(list, cancellationToken);
		}

		int result;
		try
		{
			result = await context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			throw new ConcurrencyConflictException(nameof(ShoppingList), trackedLists.FirstOrDefault()?.Identifier.Value ?? Guid.Empty, ex);
		}

		foreach (var list in trackedLists)
		{
			list.MarkCommitted();
		}

		return result;
	}
}
