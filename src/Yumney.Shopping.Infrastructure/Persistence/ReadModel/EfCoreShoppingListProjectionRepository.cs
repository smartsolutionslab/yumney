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
		var query = context.ShoppingListSummaryReadItems.Where(s => s.OwnerId == owner.Value);

		query = ApplySorting(query, sorting);

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.Skip(paging.Skip)
			.Take(paging.PageSize.Value)
			.Select(s => new ShoppingListSummary(
				ShoppingListIdentifier.From(s.Id),
				ShoppingListTitle.From(s.Title),
				ItemCount.From(s.ItemCount),
				s.CreatedAt))
			.ToListAsync(cancellationToken);

		return (items, ItemCount.From(totalCount));
	}

	public async Task<ShoppingListProjectedDetail> GetByIdAsync(
		ShoppingListIdentifier identifier,
		CancellationToken cancellationToken = default)
	{
		var summary = await context.ShoppingListSummaryReadItems
			.FirstOrDefaultAsync(s => s.Id == identifier.Value, cancellationToken)
			?? throw new EntityNotFoundException(nameof(ShoppingList), identifier.Value);

		var items = await context.ShoppingListItemReadItems
			.Where(i => i.ListId == identifier.Value)
			.OrderBy(i => i.CreatedAt)
			.Select(i => new ShoppingListItemDto(i.Id, i.Name, i.QuantityAmount, i.QuantityUnit, i.IsChecked))
			.ToListAsync(cancellationToken);

		var dto = new ShoppingListDetailDto(
			summary.Id,
			summary.Title,
			summary.RecipeIdentifier,
			summary.CreatedAt,
			items);

		return new ShoppingListProjectedDetail(summary.OwnerId, dto);
	}

	public async Task<IReadOnlyList<ShoppingListIdentifier>> FindIdsByRecipeAsync(
		OwnerIdentifier owner,
		RecipeReference recipeReference,
		CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;
		var recipeValue = recipeReference.Value;
		var ids = await context.ShoppingListSummaryReadItems
			.Where(s => s.OwnerId == ownerValue && s.RecipeIdentifier == recipeValue)
			.Select(s => s.Id)
			.ToListAsync(cancellationToken);
		return ids.Select(ShoppingListIdentifier.From).ToList();
	}

	private static IQueryable<ShoppingListSummaryReadItem> ApplySorting(
		IQueryable<ShoppingListSummaryReadItem> query,
		SortingOptions<ShoppingListSortField> sorting)
	{
		return (sorting.SortBy, sorting.Direction) switch
		{
			(ShoppingListSortField.Title, SortDirection.Ascending) => query.OrderBy(s => s.Title),
			(ShoppingListSortField.Title, SortDirection.Descending) => query.OrderByDescending(s => s.Title),
			(ShoppingListSortField.Date, SortDirection.Ascending) => query.OrderBy(s => s.CreatedAt),
			(ShoppingListSortField.Date, SortDirection.Descending) => query.OrderByDescending(s => s.CreatedAt),
			_ => throw new InvalidOperationException($"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
		};
	}
}
