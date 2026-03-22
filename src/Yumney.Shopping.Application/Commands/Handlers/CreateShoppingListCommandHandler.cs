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

        var owner = new OwnerIdentifier(currentUser.UserId);

        var items = itemCommands
            .Select(i => Domain.ShoppingList.ShoppingListItem.Create(i.Name, i.Amount, i.Unit))
            .ToList();

        var shoppingList = ShoppingList.Create(title, owner, items, recipeReference);

        await shoppingLists.AddAsync(shoppingList, cancellationToken);

        LogShoppingListCreated(shoppingList.Id.Value, title.Value);

        var itemDtos = shoppingList.Items
            .Select(i => new ShoppingListItemDto(i.Id, i.Name.Value, i.Amount?.Value, i.Unit?.Value, i.IsChecked))
            .ToList();

        return Result<ShoppingListDetailDto>.Success(
            new ShoppingListDetailDto(
                shoppingList.Id.Value,
                title.Value,
                shoppingList.RecipeReference?.Value,
                shoppingList.CreatedAt,
                itemDtos));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Shopping list {ShoppingListId} created: {Title}")]
    private partial void LogShoppingListCreated(Guid shoppingListId, string title);
}
