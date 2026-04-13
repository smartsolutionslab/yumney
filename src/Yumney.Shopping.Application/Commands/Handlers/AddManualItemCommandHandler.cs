using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class AddManualItemCommandHandler(
    IShoppingEventStore eventStore,
    ICurrentUser currentUser) : ICommandHandler<AddManualItemCommand, Result<AddedItemDto>>
{
    public async Task<Result<AddedItemDto>> HandleAsync(AddManualItemCommand command, CancellationToken cancellationToken = default)
    {
        var (itemName, explicitQuantity) = command;
        var name = itemName.Value;
        var ownerId = OwnerIdentifier.From(currentUser.UserId);

        var quantity = explicitQuantity ?? ResolveDefaultQuantity(name);
        var category = IngredientCategoryResolver.Resolve(name) ?? IngredientCategory.Other;

        var ledger = await eventStore.LoadAsync(ownerId, cancellationToken)
            ?? ShoppingLedger.Create(ownerId);

        ledger.AddItem(itemName, quantity, ItemSource.Manual);

        await eventStore.SaveAsync(ledger, cancellationToken);

        return new AddedItemDto(
            name,
            quantity.Amount,
            quantity.Unit?.Value,
            category.Value,
            ItemSource.Manual.Value,
            ledger.Identifier);
    }

    private static Quantity ResolveDefaultQuantity(string itemName)
    {
        var defaultQty = DefaultQuantityResolver.Resolve(itemName);
        return Quantity.Of(Amount.From(defaultQty.Amount), Unit.FromNullable(defaultQty.Unit));
    }
}
