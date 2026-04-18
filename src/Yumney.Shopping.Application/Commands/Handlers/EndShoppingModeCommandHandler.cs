using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class EndShoppingModeCommandHandler(
	IShoppingEventStore eventStore,
	ICurrentUser currentUser) : ICommandHandler<EndShoppingModeCommand, Result>
{
	public async Task<Result> HandleAsync(EndShoppingModeCommand command, CancellationToken cancellationToken = default)
	{
		var ledger = await eventStore.LoadAsync(OwnerIdentifier.From(currentUser.UserId), cancellationToken);
		if (ledger is null) return Result.Success();

		ledger.EndShoppingMode(command.AcceptPendingChanges);

		await eventStore.SaveAsync(ledger, cancellationToken);

		return Result.Success();
	}
}
