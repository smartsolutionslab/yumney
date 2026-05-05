using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class CreateShoppingListCommandHandler(
	IShoppingListEventStore eventStore,
	IShoppingItemCategorizer categorizer,
	ICurrentUser currentUser)
	: ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>>
{
	public async Task<Result<ShoppingListDetailDto>> HandleAsync(CreateShoppingListCommand command, CancellationToken cancellationToken = default)
	{
		var (title, itemCommands, recipeReference) = command;

		var owner = currentUser.AsOwner();

		var names = itemCommands.Select(item => item.Name).ToList();
		var categories = await categorizer.CategorizeManyAsync(names, cancellationToken);

		var items = itemCommands
			.Select(item => Domain.ShoppingList.ShoppingListItem.Create(
				item.Name,
				item.Quantity,
				categories.TryGetValue(item.Name, out var category) ? category : IngredientCategory.Other))
			.ToList();
		var shoppingList = ShoppingList.Create(title, owner, items, recipeReference);

		await eventStore.SaveAsync(shoppingList, cancellationToken);

		return shoppingList.ToDetailDto();
	}
}
