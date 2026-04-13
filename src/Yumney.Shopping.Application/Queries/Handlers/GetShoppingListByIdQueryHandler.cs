using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

public sealed class GetShoppingListByIdQueryHandler(
    IShoppingListRepository shoppingLists,
    ICurrentUser currentUser)
    : IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListDetailDto>>
{
    public async Task<Result<ShoppingListDetailDto>> HandleAsync(
        GetShoppingListByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var identifier = query.Identifier;

        var shoppingList = await shoppingLists.GetByIdAsync(identifier, cancellationToken);

        var owner = currentUser.AsOwner();

        if (shoppingList.Owner != owner) return GetShoppingListByIdErrors.AccessDenied;

        return shoppingList.ToDetailDto();
    }
}
