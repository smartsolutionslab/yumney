using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

#pragma warning disable SA1601
public sealed partial class GetShoppingListsQueryHandler(
    IShoppingListRepository shoppingLists,
    ICurrentUser currentUser,
    ILogger<GetShoppingListsQueryHandler> logger)
    : IQueryHandler<GetShoppingListsQuery, Result<PagedResult<ShoppingListSummaryDto>>>
{
    public async Task<Result<PagedResult<ShoppingListSummaryDto>>> HandleAsync(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (paging, sorting) = query;
        var owner = OwnerIdentifier.From(currentUser.UserId);

        LogGetShoppingLists(owner.Value, paging.Page.Value, paging.PageSize.Value);

        var (items, totalCount) = await shoppingLists.GetByOwnerAsync(owner, paging, sorting, cancellationToken);

        var dtos = items.Select(l => l.ToSummaryDto()).ToList();

        return Result<PagedResult<ShoppingListSummaryDto>>.Success(
            new PagedResult<ShoppingListSummaryDto>(dtos, totalCount, paging.Page.Value, paging.PageSize.Value));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching shopping lists for owner {OwnerId}, page {Page}, pageSize {PageSize}")]
    private partial void LogGetShoppingLists(string ownerId, int page, int pageSize);
}
