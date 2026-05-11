namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Consumer-defined contract for removing an item from the user's active
/// shopping list. Owned by Recipes (chat surface is the consumer).
/// </summary>
public interface IShoppingListItemRemover
{
	/// <summary>Remove an item from the user's active shopping list.</summary>
	/// <param name="request">Item name + optional quantity / reason.</param>
	/// <param name="cancellationToken">Cancellation propagated to the call.</param>
	/// <returns>True on success, false if the upstream rejected the request.</returns>
	Task<bool> RemoveAsync(RemoveShoppingItemRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Consumer-flavored shape of a remove-shopping-item request.</summary>
/// <param name="Name">Ingredient name.</param>
/// <param name="Quantity">Optional quantity; null removes the entire entry.</param>
/// <param name="Unit">Optional unit (g, ml, etc.).</param>
/// <param name="Reason">Optional removal reason ("bought", "skipped", etc.).</param>
public sealed record RemoveShoppingItemRequest(string Name, decimal? Quantity, string? Unit, string? Reason);
