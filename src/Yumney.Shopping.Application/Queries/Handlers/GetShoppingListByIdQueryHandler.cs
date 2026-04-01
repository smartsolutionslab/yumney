using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

#pragma warning disable SA1601
public sealed partial class GetShoppingListByIdQueryHandler(
#pragma warning restore SA1601
    IShoppingListRepository shoppingLists,
    ICurrentUser currentUser,
    ILogger<GetShoppingListByIdQueryHandler> logger)
    : IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListDetailDto>>
{
    public async Task<Result<ShoppingListDetailDto>> HandleAsync(
        GetShoppingListByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var identifier = query.Identifier;

        LogGetShoppingListById(identifier.Value);

        var shoppingList = await shoppingLists.GetByIdAsync(identifier, cancellationToken);

        if (shoppingList is null) return Result<ShoppingListDetailDto>.Failure(GetShoppingListByIdErrors.NotFound);

        var owner = OwnerIdentifier.From(currentUser.UserId);

        if (shoppingList.Owner != owner) return Result<ShoppingListDetailDto>.Failure(GetShoppingListByIdErrors.AccessDenied);

        return shoppingList.ToDetailDto();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching shopping list {ShoppingListId}")]
    private partial void LogGetShoppingListById(Guid shoppingListId);
}
