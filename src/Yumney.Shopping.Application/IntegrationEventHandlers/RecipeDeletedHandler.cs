using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;

/// <summary>
/// Cross-module reaction to <see cref="RecipeDeletedIntegrationEvent"/>.
/// Nulls out the <c>RecipeReference</c> on every shopping list owned by the
/// publishing user that still references the deleted recipe. The user's list
/// and items are preserved — only the broken link is severed.
/// </summary>
public sealed class RecipeDeletedHandler(IShoppingListProjectionRepository projection, IShoppingListEventStore eventStore)
	: IIntegrationEventHandler<RecipeDeletedIntegrationEvent>
{
	/// <inheritdoc />
	public async Task HandleAsync(RecipeDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var owner = OwnerIdentifier.From(@event.OwnerId);
		var reference = RecipeReference.From(@event.RecipeIdentifier);

		var listIds = await projection.FindIdsByRecipeAsync(owner, reference, cancellationToken);
		if (listIds.Count == 0) return;

		foreach (var listId in listIds)
		{
			var list = await eventStore.LoadAsync(listId, cancellationToken);
			if (list is null) continue;
			list.ClearRecipeReference();
			await eventStore.SaveAsync(list, cancellationToken);
		}
	}
}
