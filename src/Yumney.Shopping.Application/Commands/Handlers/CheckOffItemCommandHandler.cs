using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class CheckOffItemCommandHandler(
    IShoppingListRepository shoppingLists,
    ICurrentUser currentUser)
    : ICommandHandler<CheckOffItemCommand, Result>
{
    public async Task<Result> HandleAsync(CheckOffItemCommand command, CancellationToken cancellationToken = default)
    {
        var (listIdentifier, itemId, isChecked) = command;

        var shoppingList = await shoppingLists.GetByIdForUpdateAsync(listIdentifier, cancellationToken);

        var owner = currentUser.AsOwner();

        if (shoppingList.Owner != owner) return Result.Failure(CheckOffItemErrors.AccessDenied);

        if (isChecked)
        {
            shoppingList.CheckOffItem(itemId);
        }
        else
        {
            shoppingList.UncheckItem(itemId);
        }

        await shoppingLists.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
