using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class AddManualItemCommandHandler(
    IShoppingEventStore eventStore,
    ICurrentUser currentUser,
    ILogger<AddManualItemCommandHandler> logger) : ICommandHandler<AddManualItemCommand, Result<AddedItemDto>>
{
    public async Task<Result<AddedItemDto>> HandleAsync(AddManualItemCommand command, CancellationToken cancellationToken = default)
    {
        var (itemName, explicitQuantity, explicitUnit) = command;
        var ownerId = currentUser.UserId;

        LogAddManualItem(ownerId, itemName);

        var resolved = ResolveQuantity(itemName, explicitQuantity, explicitUnit);
        var category = IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other;

        var ledger = await eventStore.LoadAsync(ownerId, cancellationToken)
            ?? ShoppingLedger.Create(ownerId);

        ledger.AddItem(itemName, resolved.Quantity, resolved.Unit, "manual");

        await eventStore.SaveAsync(ledger, cancellationToken);

        return new AddedItemDto(
            itemName,
            resolved.Quantity,
            resolved.Unit,
            category.Value,
            "manual",
            ledger.Id);
    }

    private static (decimal Quantity, string? Unit) ResolveQuantity(string itemName, decimal? explicitQuantity, string? explicitUnit)
    {
        if (explicitQuantity.HasValue)
            return (explicitQuantity.Value, explicitUnit);

        var defaultQty = DefaultQuantityResolver.Resolve(itemName);
        return (defaultQty.Amount, defaultQty.Unit);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Adding manual item for owner {OwnerId}: {ItemName}")]
    private partial void LogAddManualItem(string ownerId, string itemName);
}
