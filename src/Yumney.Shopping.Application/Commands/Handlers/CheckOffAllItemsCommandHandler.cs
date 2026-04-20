using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class CheckOffAllItemsCommandHandler(IShoppingUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<CheckOffAllItemsCommand, Result>
{
	public async Task<Result> HandleAsync(CheckOffAllItemsCommand command, CancellationToken cancellationToken = default)
	{
		var (listIdentifier, isChecked) = command;

		var shoppingList = await unitOfWork.ShoppingLists.GetByIdForUpdateAsync(listIdentifier, cancellationToken);

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

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success();
	}
}
