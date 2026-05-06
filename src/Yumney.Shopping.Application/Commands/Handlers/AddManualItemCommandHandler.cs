using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

public sealed class AddManualItemCommandHandler(IShoppingEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<AddManualItemCommand, Result<AddedItemDto>>
{
	public async Task<Result<AddedItemDto>> HandleAsync(AddManualItemCommand command, CancellationToken cancellationToken = default)
	{
		var (itemName, explicitQuantity, source) = command;
		var owner = currentUser.AsOwner();

		var quantity = explicitQuantity ?? DefaultQuantityResolver.Resolve(itemName.Value);
		var category = IngredientCategoryResolver.Resolve(itemName.Value) ?? IngredientCategory.Other;

		var ledger = await eventStore.FindAsync(owner, cancellationToken) ?? ShoppingLedger.Create(owner);
		ledger.AddItem(itemName, quantity, source);

		await eventStore.SaveAsync(ledger, cancellationToken);

		return ledger.ToAddedItemDto(itemName, quantity, category, source);
	}
}
