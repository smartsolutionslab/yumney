using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class CheckOffAllItemsCommandHandler(
    IShoppingListRepository shoppingLists,
    ICurrentUser currentUser,
    ILogger<CheckOffAllItemsCommandHandler> logger)
    : ICommandHandler<CheckOffAllItemsCommand, Result>
{
    public async Task<Result> HandleAsync(CheckOffAllItemsCommand command, CancellationToken cancellationToken = default)
    {
        var (listIdentifier, isChecked) = command;

        var shoppingList = await shoppingLists.GetByIdForUpdateAsync(listIdentifier, cancellationToken);

        if (shoppingList is null)
        {
            return Result.Failure(CheckOffItemErrors.ListNotFound);
        }

        var owner = new OwnerIdentifier(currentUser.UserId);

        if (shoppingList.Owner != owner)
        {
            return Result.Failure(CheckOffItemErrors.AccessDenied);
        }

        if (isChecked)
        {
            shoppingList.CheckAllItems();
        }
        else
        {
            shoppingList.UncheckAllItems();
        }

        await shoppingLists.SaveChangesAsync(cancellationToken);

        LogAllItemsChecked(listIdentifier.Value, isChecked);

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "All items in shopping list {ShoppingListId} set to checked={IsChecked}")]
    private partial void LogAllItemsChecked(Guid shoppingListId, bool isChecked);
}
