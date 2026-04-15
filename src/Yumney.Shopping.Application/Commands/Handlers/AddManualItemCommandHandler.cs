using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class AddManualItemCommandHandler(
    IShoppingEventStore eventStore,
    ICurrentUser currentUser,
    ILogger<AddManualItemCommandHandler> logger) : ICommandHandler<AddManualItemCommand, Result<AddedItemDto>>
{
    public async Task<Result<AddedItemDto>> HandleAsync(AddManualItemCommand command, CancellationToken cancellationToken = default)
    {
        var (itemName, explicitQuantity) = command;
        var name = itemName.Value;
        var ownerId = currentUser.UserId;

        LogAddManualItem(ownerId, name);

        var quantity = explicitQuantity ?? ResolveDefaultQuantity(name);
        var category = IngredientCategoryResolver.Resolve(name) ?? IngredientCategory.Other;

        var ledger = await eventStore.LoadAsync(ownerId, cancellationToken)
            ?? ShoppingLedger.Create(ownerId);

        ledger.AddItem(itemName, quantity, ItemSources.Manual);

        await eventStore.SaveAsync(ledger, cancellationToken);

        return new AddedItemDto(
            name,
            quantity.Amount,
            quantity.Unit?.Value,
            category.Value,
            ItemSources.Manual,
            ledger.Id);
    }

    private static Quantity ResolveDefaultQuantity(string itemName)
    {
        var defaultQty = DefaultQuantityResolver.Resolve(itemName);
        return Quantity.Of(Amount.From(defaultQty.Amount), Unit.FromNullable(defaultQty.Unit));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Adding manual item for owner {OwnerId}: {ItemName}")]
    private partial void LogAddManualItem(string ownerId, string itemName);
}
