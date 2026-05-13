using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class CheckOffAllItemsCommandHandler(IShoppingListEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<CheckOffAllItemsCommand, Result>
{
	public async Task<Result> HandleAsync(CheckOffAllItemsCommand command, CancellationToken cancellationToken = default)
	{
		var (listIdentifier, isChecked) = command;
		var shoppingList = await eventStore.LoadAsync(listIdentifier, cancellationToken);
		var owner = currentUser.AsOwner();

		if (shoppingList.Owner != owner) return Result.Failure(CheckOffItemErrors.AccessDenied);

		if (isChecked)
		{
			shoppingList.CheckAllItems();
		}
		else
		{
			shoppingList.UncheckAllItems();
		}

		await eventStore.SaveAsync(shoppingList, cancellationToken);

		return Result.Success();
	}
}
