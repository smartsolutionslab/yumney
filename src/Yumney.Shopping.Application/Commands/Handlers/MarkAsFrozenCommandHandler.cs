using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class MarkAsFrozenCommandHandler(IShoppingEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<MarkAsFrozenCommand, Result>
{
	public async Task<Result> HandleAsync(MarkAsFrozenCommand command, CancellationToken cancellationToken = default)
	{
		var (itemName, unit) = command;
		var owner = currentUser.AsOwner();

		var ledger = await eventStore.FindAsync(owner, cancellationToken);
		if (ledger is null) return Result.Success();

		ledger.MarkAsFrozen(itemName, unit);
		await eventStore.SaveAsync(ledger, cancellationToken);

		return Result.Success();
	}
}
