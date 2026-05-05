using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class ChangeItemCategoryCommandHandler(IShoppingListEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<ChangeItemCategoryCommand, Result>
{
	public async Task<Result> HandleAsync(ChangeItemCategoryCommand command, CancellationToken cancellationToken = default)
	{
		var (listIdentifier, itemId, category) = command;

		var shoppingList = await eventStore.LoadAsync(listIdentifier, cancellationToken)
			?? throw new EntityNotFoundException(nameof(ShoppingList), listIdentifier.Value);

		if (shoppingList.Owner != currentUser.AsOwner()) return Result.Failure(CheckOffItemErrors.AccessDenied);

		shoppingList.ChangeItemCategory(itemId, category);

		await eventStore.SaveAsync(shoppingList, cancellationToken);

		return Result.Success();
	}
}
