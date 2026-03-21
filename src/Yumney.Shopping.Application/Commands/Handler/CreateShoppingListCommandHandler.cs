using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handler;

#pragma warning disable SA1601
public sealed partial class CreateShoppingListCommandHandler(
    IShoppingListRepository shoppingLists,
    ICurrentUser currentUser,
    ILogger<CreateShoppingListCommandHandler> logger)
    : ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>>
{
    public async Task<Result<ShoppingListDetailDto>> HandleAsync(CreateShoppingListCommand command, CancellationToken cancellationToken = default)
    {
        var (title, itemCommands, recipeIdentifier) = command;

        var owner = new OwnerIdentifier(currentUser.UserId);

        var items = itemCommands
            .Select(i => ShoppingListItem.Create(i.Name, i.Amount, i.Unit))
            .ToList();

        var shoppingList = ShoppingList.Create(title, owner, items, recipeIdentifier);

        await shoppingLists.AddAsync(shoppingList, cancellationToken);

        LogShoppingListCreated(shoppingList.Id, title.Value);

        var itemDtos = shoppingList.Items
            .Select(i => new ShoppingListItemDto(i.Name.Value, i.Amount?.Value, i.Unit?.Value))
            .ToList();

        return Result<ShoppingListDetailDto>.Success(
            new ShoppingListDetailDto(
                shoppingList.Id,
                title.Value,
                recipeIdentifier,
                shoppingList.CreatedAt,
                itemDtos));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Shopping list {ShoppingListId} created: {Title}")]
    private partial void LogShoppingListCreated(Guid shoppingListId, string title);
}
