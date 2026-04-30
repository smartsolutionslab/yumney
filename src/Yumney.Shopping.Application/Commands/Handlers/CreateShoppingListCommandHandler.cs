using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class CreateShoppingListCommandHandler(IShoppingListEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>>
{
	public async Task<Result<ShoppingListDetailDto>> HandleAsync(CreateShoppingListCommand command, CancellationToken cancellationToken = default)
	{
		var (title, itemCommands, recipeReference) = command;

		var owner = currentUser.AsOwner();

		var items = itemCommands
			.Select(item => Domain.ShoppingList.ShoppingListItem.Create(item.Name, item.Quantity))
			.ToList();
		var shoppingList = ShoppingList.Create(title, owner, items, recipeReference);

		await eventStore.SaveAsync(shoppingList, cancellationToken);

		return shoppingList.ToDetailDto();
	}
}
