using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class CreateShoppingListCommandHandler(
    IShoppingListRepository shoppingLists,
    ICurrentUser currentUser,
    ILogger<CreateShoppingListCommandHandler> logger)
    : ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>>
{
    public async Task<Result<ShoppingListDetailDto>> HandleAsync(CreateShoppingListCommand command, CancellationToken cancellationToken = default)
    {
        var (title, itemCommands, recipeReference) = command;

        var owner = OwnerIdentifier.From(currentUser.UserId);

        var items = itemCommands
            .Select(i => Domain.ShoppingList.ShoppingListItem.Create(i.Name, i.Quantity))
            .ToList();
        var shoppingList = ShoppingList.Create(title, owner, items, recipeReference);

        await shoppingLists.AddAsync(shoppingList, cancellationToken);

        LogShoppingListCreated(shoppingList.Id.Value, title.Value);

        return shoppingList.ToDetailDto();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Shopping list {ShoppingListId} created: {Title}")]
    private partial void LogShoppingListCreated(Guid shoppingListId, string title);
}
