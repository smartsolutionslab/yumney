using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class RemoveShoppingItemCommandHandler(
    IShoppingEventStore eventStore,
    ICurrentUser currentUser) : ICommandHandler<RemoveShoppingItemCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveShoppingItemCommand command, CancellationToken cancellationToken = default)
    {
        var (itemName, quantityOverride, unit, reason) = command;
        var ownerId = currentUser.UserId;

        var ledger = await eventStore.LoadAsync(ownerId, cancellationToken);
        if (ledger is null) return Result.Success();

        var name = itemName.Value;
        var quantity = Amount.From(quantityOverride ?? DefaultQuantityResolver.Resolve(name).Amount);
        ledger.RemoveItem(itemName, quantity, Unit.FromNullable(unit), reason);

        await eventStore.SaveAsync(ledger, cancellationToken);

        return Result.Success();
    }
}
