using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class CheckOffItemCommandHandler(
    IShoppingListRepository shoppingLists,
    ICurrentUser currentUser,
    ILogger<CheckOffItemCommandHandler> logger)
    : ICommandHandler<CheckOffItemCommand, Result>
{
    public async Task<Result> HandleAsync(CheckOffItemCommand command, CancellationToken cancellationToken = default)
    {
        var (listIdentifier, itemId, isChecked) = command;

        var shoppingList = await shoppingLists.GetByIdForUpdateAsync(listIdentifier, cancellationToken);

        if (shoppingList is null)
        {
            return Result.Failure(CheckOffItemErrors.ListNotFound);
        }

        var owner = OwnerIdentifier.From(currentUser.UserId);

        if (shoppingList.Owner != owner)
        {
            return Result.Failure(CheckOffItemErrors.AccessDenied);
        }

        if (isChecked)
        {
            shoppingList.CheckOffItem(itemId);
        }
        else
        {
            shoppingList.UncheckItem(itemId);
        }

        await shoppingLists.SaveChangesAsync(cancellationToken);

        LogItemCheckedOff(listIdentifier.Value, itemId, isChecked);

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Item {ItemId} in shopping list {ShoppingListId} set to checked={IsChecked}")]
    private partial void LogItemCheckedOff(Guid shoppingListId, ShoppingListItemIdentifier itemId, bool isChecked);
}
