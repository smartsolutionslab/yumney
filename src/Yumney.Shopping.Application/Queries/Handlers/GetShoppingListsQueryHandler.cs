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
    : IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ShoppingListSummaryDto>>> HandleAsync(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken = default)
    {
        var owner = new OwnerIdentifier(currentUser.UserId);

        LogGetShoppingLists(owner.Value);

        var lists = await shoppingLists.GetByOwnerAsync(owner, cancellationToken);

        var dtos = lists
            .Select(l => new ShoppingListSummaryDto(
                l.Id.Value,
                l.Title.Value,
                l.Items.Count,
                l.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<ShoppingListSummaryDto>>.Success(dtos);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching shopping lists for owner {OwnerId}")]
    private partial void LogGetShoppingLists(string ownerId);
}
