using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class StartShoppingModeCommandHandler(IShoppingEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<StartShoppingModeCommand, Result>
{
	public async Task<Result> HandleAsync(StartShoppingModeCommand command, CancellationToken cancellationToken = default)
	{
		var ownerId = OwnerIdentifier.From(currentUser.UserId);
		var ledger = await eventStore.LoadAsync(ownerId, cancellationToken) ?? ShoppingLedger.Create(ownerId);

		ledger.StartShoppingMode();

		await eventStore.SaveAsync(ledger, cancellationToken);

		return Result.Success();
	}
}
