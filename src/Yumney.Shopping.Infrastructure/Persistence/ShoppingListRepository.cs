using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
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

	public async Task<ShoppingList> GetByIdAsync(
		ShoppingListIdentifier identifier,
		CancellationToken cancellationToken = default)
	{
		return await shoppingLists
			.AsNoTracking()
			.Include(l => l.Items)
			.FirstOrDefaultAsync(l => l.Id == identifier, cancellationToken)
			?? throw new EntityNotFoundException(nameof(ShoppingList), identifier.Value);
	}

	public async Task<ShoppingList> GetByIdForUpdateAsync(
		ShoppingListIdentifier identifier,
		CancellationToken cancellationToken = default)
	{
		return await shoppingLists
			.Include(l => l.Items)
			.FirstOrDefaultAsync(l => l.Id == identifier, cancellationToken)
			?? throw new EntityNotFoundException(nameof(ShoppingList), identifier.Value);
	}

	public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task<(IReadOnlyList<ShoppingListSummary> Items, ItemCount TotalCount)> GetByOwnerAsync(
		OwnerIdentifier owner,
		PagingOptions paging,
		SortingOptions<ShoppingListSortField> sorting,
		CancellationToken cancellationToken = default)
	{
		var query = shoppingLists.AsNoTracking().Where(l => l.Owner == owner);

		query = ApplySorting(query, sorting);

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.Skip(paging.Skip)
			.Take(paging.PageSize.Value)
			.Select(l => new ShoppingListSummary(l.Id, l.Title, ItemCount.From(l.Items.Count), l.CreatedAt))
			.ToListAsync(cancellationToken);

		return (items, ItemCount.From(totalCount));
	}

	private static IQueryable<ShoppingList> ApplySorting(
		IQueryable<ShoppingList> query,
		SortingOptions<ShoppingListSortField> sorting)
	{
		return (sorting.SortBy, sorting.Direction) switch
		{
			(ShoppingListSortField.Title, SortDirection.Ascending) => query.OrderBy(l => l.Title),
			(ShoppingListSortField.Title, SortDirection.Descending) => query.OrderByDescending(l => l.Title),
			(ShoppingListSortField.Date, SortDirection.Ascending) => query.OrderBy(l => l.CreatedAt),
			(ShoppingListSortField.Date, SortDirection.Descending) => query.OrderByDescending(l => l.CreatedAt),
			_ => throw new InvalidOperationException($"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
		};
	}
}
