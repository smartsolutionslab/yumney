using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries;

public sealed record GetShoppingListByIdQuery(
    ShoppingListIdentifier Identifier) : IQuery<Result<ShoppingListDetailDto>>
{
    public static GetShoppingListByIdQuery From(Guid identifier)
    {
        return new GetShoppingListByIdQuery(new ShoppingListIdentifier(identifier));
    }
}
