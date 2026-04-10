using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class AddManualItemCommandHandler(
    IShoppingLedgerRepository ledgers,
    ICurrentUser currentUser,
    ILogger<AddManualItemCommandHandler> logger) : ICommandHandler<AddManualItemCommand, Result<AddedItemDto>>
{
    public async Task<Result<AddedItemDto>> HandleAsync(AddManualItemCommand command, CancellationToken cancellationToken = default)
    {
        var (itemName, explicitQuantity, explicitUnit) = command;
        var owner = currentUser.AsOwner();

        LogAddManualItem(owner.Value, itemName);

        var resolved = ResolveQuantity(itemName, explicitQuantity, explicitUnit);
        var category = IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other;

        var ledger = await ledgers.FindByOwnerAsync(owner, cancellationToken);
        if (ledger is null)
        {
            ledger = ShoppingLedger.Create(owner);
            var tx = ledger.AddItem(ItemName.From(itemName), resolved.Quantity, resolved.Unit, TransactionSource.Manual);
            await ledgers.AddAsync(ledger, cancellationToken);
            return ToDto(itemName, resolved, category, tx);
        }

        var transaction = ledger.AddItem(ItemName.From(itemName), resolved.Quantity, resolved.Unit, TransactionSource.Manual);
        await ledgers.SaveChangesAsync(cancellationToken);
        return ToDto(itemName, resolved, category, transaction);
    }

    private static Result<AddedItemDto> ToDto(
        string itemName,
        (decimal Quantity, string? Unit) resolved,
        IngredientCategory category,
        LedgerTransaction tx)
    {
        return new AddedItemDto(itemName, resolved.Quantity, resolved.Unit, category.Value, tx.Source.Value, tx.Id.Value);
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
