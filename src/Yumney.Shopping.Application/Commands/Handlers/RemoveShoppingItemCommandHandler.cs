using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Application.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class RemoveShoppingItemCommandHandler(IShoppingEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<RemoveShoppingItemCommand, Result>
{
	public async Task<Result> HandleAsync(RemoveShoppingItemCommand command, CancellationToken cancellationToken = default)
	{
		var (itemName, explicitQuantity, reason) = command;
		var ownerId = OwnerIdentifier.From(currentUser.UserId);

		var ledger = await eventStore.LoadAsync(ownerId, cancellationToken);
		if (ledger is null) return Result.Success();

		var quantity = explicitQuantity ?? DefaultQuantityResolver.Resolve(itemName.Value);
		ledger.RemoveItem(itemName, quantity, reason);

		await eventStore.SaveAsync(ledger, cancellationToken);

		return Result.Success();
	}
}
