using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

public sealed class GetShoppingListsQueryHandler(
	IShoppingListRepository shoppingLists,
	IShoppingListProjectionRepository projection,
	ShoppingOptions options,
	ICurrentUser currentUser)
	: IQueryHandler<GetShoppingListsQuery, Result<PagedResult<ShoppingListSummaryDto>>>
{
	public async Task<Result<PagedResult<ShoppingListSummaryDto>>> HandleAsync(
		GetShoppingListsQuery query,
		CancellationToken cancellationToken = default)
	{
		var (paging, sorting) = query;
		var owner = currentUser.AsOwner();

		var (items, totalCount) = options.UseProjectionReadModel
			? await projection.GetByOwnerAsync(owner, paging, sorting, cancellationToken)
			: await shoppingLists.GetByOwnerAsync(owner, paging, sorting, cancellationToken);

		var shoppingListSummaryDtos = items.Select(l => l.ToSummaryDto()).ToList();

		return PagedResultExtensions.With(shoppingListSummaryDtos, totalCount, paging);
	}
}
