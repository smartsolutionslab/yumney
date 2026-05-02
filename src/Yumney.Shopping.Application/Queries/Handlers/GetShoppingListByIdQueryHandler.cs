using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

public sealed class GetShoppingListByIdQueryHandler(IShoppingListProjectionRepository projection, ICurrentUser currentUser)
	: IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListDetailDto>>
{
	public async Task<Result<ShoppingListDetailDto>> HandleAsync(
		GetShoppingListByIdQuery query,
		CancellationToken cancellationToken = default)
	{
		var detail = await projection.GetByIdAsync(query.Identifier, cancellationToken);
		if (detail.OwnerId != currentUser.UserId) return GetShoppingListByIdErrors.AccessDenied;
		return detail.Dto;
	}
}
