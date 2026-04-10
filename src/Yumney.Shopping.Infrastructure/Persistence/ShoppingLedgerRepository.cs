using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingLedgerRepository(ShoppingDbContext context) : IShoppingLedgerRepository
{
#pragma warning disable SA1311
    private readonly DbSet<ShoppingLedger> ledgers = context.ShoppingLedgers;
#pragma warning restore SA1311

    public async Task<ShoppingLedger?> FindByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
    {
        return await ledgers
            .AsNoTracking()
            .Include(l => l.Transactions)
            .FirstOrDefaultAsync(l => l.Owner == owner, cancellationToken);
    }

    public async Task<ShoppingLedger> GetByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
    {
        return await ledgers
            .Include(l => l.Transactions)
            .FirstOrDefaultAsync(l => l.Owner == owner, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ShoppingLedger), owner.Value);
    }

    public async Task AddAsync(ShoppingLedger ledger, CancellationToken cancellationToken = default)
    {
        await ledgers.AddAsync(ledger, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
