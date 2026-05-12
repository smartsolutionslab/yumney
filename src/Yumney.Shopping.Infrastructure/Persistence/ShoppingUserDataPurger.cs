using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

/// <summary>
/// EF-backed implementation of <see cref="IShoppingUserDataPurger"/>. Resolves
/// the owner's aggregate IDs from the metadata tables, deletes their event
/// streams, then drops the metadata rows and every owner-scoped read-model row.
/// </summary>
public sealed class ShoppingUserDataPurger(ShoppingDbContext writeContext, ShoppingReadDbContext readContext)
	: IShoppingUserDataPurger
{
	public async Task PurgeAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;

		var shoppingListAggregateIds = await writeContext.ShoppingListAggregates
			.Where(aggregate => aggregate.OwnerId == ownerValue)
			.Select(aggregate => aggregate.AggregateId)
			.ToListAsync(cancellationToken);

		if (shoppingListAggregateIds.Count > 0)
		{
			await writeContext.ShoppingListEvents
				.Where(stored => shoppingListAggregateIds.Contains(stored.AggregateId))
				.ExecuteDeleteAsync(cancellationToken);
		}

		await writeContext.ShoppingListAggregates
			.Where(aggregate => aggregate.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);

		var shoppingAggregateIds = await writeContext.ShoppingAggregates
			.Where(aggregate => aggregate.OwnerId == ownerValue)
			.Select(aggregate => aggregate.AggregateId)
			.ToListAsync(cancellationToken);

		if (shoppingAggregateIds.Count > 0)
		{
			await writeContext.ShoppingEvents
				.Where(stored => shoppingAggregateIds.Contains(stored.AggregateId))
				.ExecuteDeleteAsync(cancellationToken);
		}

		await writeContext.ShoppingAggregates
			.Where(aggregate => aggregate.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);

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
