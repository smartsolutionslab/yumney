using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public sealed class EfCoreShoppingListProjectionRepository(ShoppingReadDbContext context)
	: IShoppingListProjectionRepository
{
	public async Task<(IReadOnlyList<ShoppingListSummary> Items, ItemCount TotalCount)> GetByOwnerAsync(
		OwnerIdentifier owner,
		PagingOptions paging,
		SortingOptions<ShoppingListSortField> sorting,
		CancellationToken cancellationToken = default)
	{
		var query = context.ShoppingListSummaryReadItems.Where(summary => summary.OwnerId == owner.Value);

		query = ApplySorting(query, sorting);

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.Skip(paging.Skip)
			.Take(paging.PageSize.Value)
			.Select(summary => new ShoppingListSummary(
				ShoppingListIdentifier.From(summary.Id),
				ShoppingListTitle.From(summary.Title),
				ItemCount.From(summary.ItemCount),
				summary.CreatedAt))
			.ToListAsync(cancellationToken);

		return (items, ItemCount.From(totalCount));
	}

	public async Task<ShoppingListProjectedDetail> GetByIdAsync(
		ShoppingListIdentifier identifier,
		CancellationToken cancellationToken = default)
	{
		var summary = await context.ShoppingListSummaryReadItems
			.FirstOrDefaultAsync(row => row.Id == identifier.Value, cancellationToken)
			?? throw new EntityNotFoundException(nameof(ShoppingList), identifier.Value);

		var itemRows = await context.ShoppingListItemReadItems
			.Where(item => item.ListId == identifier.Value)
			.OrderBy(item => item.CreatedAt)
			.ToListAsync(cancellationToken);

		return new ShoppingListProjectedDetail(summary.OwnerId, summary.ToDetailDto(itemRows.ToDtos()));
	}

	public async Task<IReadOnlyList<ShoppingListIdentifier>> FindIdsByRecipeAsync(
		OwnerIdentifier owner,
		RecipeReference recipeReference,
		CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;
		var recipeValue = recipeReference.Value;
		var ids = await context.ShoppingListSummaryReadItems
			.Where(summary => summary.OwnerId == ownerValue && summary.RecipeIdentifier == recipeValue)
			.Select(summary => summary.Id)
			.ToListAsync(cancellationToken);
		return ids.Select(ShoppingListIdentifier.From).ToList();
	}

	private static IQueryable<ShoppingListSummaryReadItem> ApplySorting(
		IQueryable<ShoppingListSummaryReadItem> query,
		SortingOptions<ShoppingListSortField> sorting)
	{
		return (sorting.SortBy, sorting.Direction) switch
		{
			(ShoppingListSortField.Title, SortDirection.Ascending) => query.OrderBy(summary => summary.Title),
			(ShoppingListSortField.Title, SortDirection.Descending) => query.OrderByDescending(summary => summary.Title),
			(ShoppingListSortField.Date, SortDirection.Ascending) => query.OrderBy(summary => summary.CreatedAt),
			(ShoppingListSortField.Date, SortDirection.Descending) => query.OrderByDescending(summary => summary.CreatedAt),
			_ => throw new InvalidOperationException($"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
		};
	}
}
