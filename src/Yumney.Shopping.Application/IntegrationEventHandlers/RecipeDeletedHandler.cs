using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;

/// <summary>
/// Cross-module reaction to <see cref="RecipeDeletedIntegrationEvent"/>.
/// Nulls out the <c>RecipeReference</c> on every shopping list owned by the
/// publishing user that still references the deleted recipe. The user's list
/// and items are preserved — only the broken link is severed.
/// </summary>
public sealed class RecipeDeletedHandler(IShoppingUnitOfWork unitOfWork)
	: IIntegrationEventHandler<RecipeDeletedIntegrationEvent>
{
	/// <inheritdoc />
	public async Task HandleAsync(RecipeDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var owner = OwnerIdentifier.From(@event.OwnerId);
		var reference = RecipeReference.From(@event.RecipeIdentifier);

		var lists = await unitOfWork.ShoppingLists.FindByRecipeReferenceAsync(owner, reference, cancellationToken);
		if (lists.Count == 0) return;

		foreach (var list in lists)
		{
			list.ClearRecipeReference();
		}

		await unitOfWork.SaveChangesAsync(cancellationToken);
	}
}
