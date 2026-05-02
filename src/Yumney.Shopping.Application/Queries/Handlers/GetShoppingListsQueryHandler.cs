using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

public sealed class GetShoppingListsQueryHandler(IShoppingListProjectionRepository projection, ICurrentUser currentUser)
	: IQueryHandler<GetShoppingListsQuery, Result<PagedResult<ShoppingListSummaryDto>>>
{
	public async Task<Result<PagedResult<ShoppingListSummaryDto>>> HandleAsync(
		GetShoppingListsQuery query,
		CancellationToken cancellationToken = default)
	{
		var (paging, sorting) = query;
		var owner = currentUser.AsOwner();

		var (items, totalCount) = await projection.GetByOwnerAsync(owner, paging, sorting, cancellationToken);

		return PagedResultExtensions.With(items.ToSummaryDtos(), totalCount, paging);
	}
}
