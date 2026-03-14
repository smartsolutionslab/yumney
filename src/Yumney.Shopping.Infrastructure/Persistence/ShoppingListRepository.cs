using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingListRepository(ShoppingDbContext context) : IShoppingListRepository
{
    private readonly DbSet<ShoppingList> shoppingLists = context.ShoppingLists;

    public async Task AddAsync(ShoppingList shoppingList, CancellationToken cancellationToken = default)
    {
        await shoppingLists.AddAsync(shoppingList, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShoppingList?> GetByIdAsync(
        ShoppingListIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        return await shoppingLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == identifier.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingList>> GetByOwnerAsync(
        OwnerIdentifier owner,
        CancellationToken cancellationToken = default)
    {
        return await shoppingLists
            .Include(l => l.Items)
            .Where(l => l.Owner == owner)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
