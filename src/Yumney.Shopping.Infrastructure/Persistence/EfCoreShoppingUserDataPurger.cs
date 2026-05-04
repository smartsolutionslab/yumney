using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

/// <summary>
/// EF-backed implementation of <see cref="IShoppingUserDataPurger"/>. Resolves
/// the owner's aggregate IDs from the metadata tables, deletes their event
/// streams, then drops the metadata rows and every owner-scoped read-model row.
/// </summary>
public sealed class EfCoreShoppingUserDataPurger(ShoppingDbContext writeContext, ShoppingReadDbContext readContext)
	: IShoppingUserDataPurger
{
	public async Task PurgeAsync(string keycloakUserId, CancellationToken cancellationToken = default)
	{
		var shoppingListAggregateIds = await writeContext.ShoppingListAggregates
			.Where(aggregate => aggregate.OwnerId == keycloakUserId)
			.Select(aggregate => aggregate.AggregateId)
			.ToListAsync(cancellationToken);

		if (shoppingListAggregateIds.Count > 0)
		{
			await writeContext.ShoppingListEvents
				.Where(stored => shoppingListAggregateIds.Contains(stored.AggregateId))
				.ExecuteDeleteAsync(cancellationToken);
		}

		await writeContext.ShoppingListAggregates
			.Where(aggregate => aggregate.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);

		var shoppingAggregateIds = await writeContext.ShoppingAggregates
			.Where(aggregate => aggregate.OwnerId == keycloakUserId)
			.Select(aggregate => aggregate.AggregateId)
			.ToListAsync(cancellationToken);

		if (shoppingAggregateIds.Count > 0)
		{
			await writeContext.ShoppingEvents
				.Where(stored => shoppingAggregateIds.Contains(stored.AggregateId))
				.ExecuteDeleteAsync(cancellationToken);
		}

		await writeContext.ShoppingAggregates
			.Where(aggregate => aggregate.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<ShoppingListItemReadItem>()
			.Where(item => item.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<ShoppingListSummaryReadItem>()
			.Where(summary => summary.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<IngredientBalanceReadItem>()
			.Where(balance => balance.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<ShoppingLedgerReadItem>()
			.Where(ledger => ledger.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
