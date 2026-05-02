using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

public sealed class GetMergedShoppingListQueryHandler(
	IShoppingLedgerReadModelRepository readModel,
	ICurrentUser currentUser) : IQueryHandler<GetMergedShoppingListQuery, Result<MergedShoppingListDto>>
{
	public async Task<Result<MergedShoppingListDto>> HandleAsync(GetMergedShoppingListQuery query, CancellationToken cancellationToken = default)
	{
		return await readModel.GetByOwnerAsync(currentUser.AsOwner(), query.IncludePastBought, cancellationToken);
	}
}
