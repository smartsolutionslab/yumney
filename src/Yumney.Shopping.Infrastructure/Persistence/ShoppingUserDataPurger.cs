using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

/// <summary>
/// EF-backed implementation of <see cref="IShoppingUserDataPurger"/>. Drains the
/// owner's two event streams (ShoppingList aggregates + Shopping aggregates) via
/// the shared <see cref="EventSourcedAggregateDraining"/> helper, then deletes
/// every owner-scoped read-model row.
/// </summary>
public sealed class ShoppingUserDataPurger(ShoppingDbContext writeContext, ShoppingReadDbContext readContext)
	: IShoppingUserDataPurger
{
	public async Task PurgeAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;

		await writeContext.ShoppingListAggregates.DrainOwnerAggregatesAsync(
			writeContext.ShoppingListEvents,
			ownerValue,
			cancellationToken);

		await writeContext.ShoppingAggregates.DrainOwnerAggregatesAsync(
			writeContext.ShoppingEvents,
			ownerValue,
			cancellationToken);

		await readContext.Set<ShoppingListItemReadItem>()
			.Where(item => item.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<ShoppingListSummaryReadItem>()
			.Where(summary => summary.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<IngredientBalanceReadItem>()
			.Where(balance => balance.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<ShoppingLedgerReadItem>()
			.Where(ledger => ledger.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
