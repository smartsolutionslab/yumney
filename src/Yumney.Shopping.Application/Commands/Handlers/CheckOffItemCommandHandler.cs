using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class CheckOffItemCommandHandler(IShoppingListEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<CheckOffItemCommand, Result>
{
	public async Task<Result> HandleAsync(CheckOffItemCommand command, CancellationToken cancellationToken = default)
	{
		var (listIdentifier, itemId, isChecked) = command;

		var shoppingList = await eventStore.LoadAsync(listIdentifier, cancellationToken)
			?? throw new EntityNotFoundException(nameof(ShoppingList), listIdentifier.Value);

		var owner = currentUser.AsOwner();

		if (shoppingList.Owner != owner) return Result.Failure(CheckOffItemErrors.AccessDenied);

		if (isChecked)
		{
			shoppingList.CheckOffItem(itemId);
		}
		else
		{
			shoppingList.UncheckItem(itemId);
		}

		await eventStore.SaveAsync(shoppingList, cancellationToken);

		return Result.Success();
	}
}
