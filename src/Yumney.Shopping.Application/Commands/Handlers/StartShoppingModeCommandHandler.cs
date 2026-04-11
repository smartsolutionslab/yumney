using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class StartShoppingModeCommandHandler(
    IShoppingEventStore eventStore,
    ICurrentUser currentUser) : ICommandHandler<StartShoppingModeCommand, Result>
{
    public async Task<Result> HandleAsync(StartShoppingModeCommand command, CancellationToken cancellationToken = default)
    {
        var ledger = await eventStore.LoadAsync(currentUser.UserId, cancellationToken)
            ?? ShoppingLedger.Create(currentUser.UserId);

        ledger.StartShoppingMode();

        await eventStore.SaveAsync(ledger, cancellationToken);

        return Result.Success();
    }
}
