using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class RemoveShoppingItemCommandHandler(
    IShoppingEventStore eventStore,
    ICurrentUser currentUser) : ICommandHandler<RemoveShoppingItemCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(RemoveShoppingItemCommand command, CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.UserId;

        var ledger = await eventStore.LoadAsync(ownerId, cancellationToken);
        if (ledger is null)
            return Result.Success();

        var name = command.ItemName.Value;
        var quantity = command.Quantity ?? DefaultQuantityResolver.Resolve(name).Amount;
        ledger.RemoveItem(name, quantity, command.Unit, command.Reason);

        await eventStore.SaveAsync(ledger, cancellationToken);

        return Result.Success();
    }
}
