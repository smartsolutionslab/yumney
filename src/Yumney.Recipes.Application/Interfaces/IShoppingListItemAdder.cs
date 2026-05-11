namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Consumer-defined contract for adding a manual item to the user's active
/// shopping list. Owned by Recipes (chat surface is the consumer) per
/// CLAUDE.md cross-module rule + ADR 0002.
/// </summary>
public interface IShoppingListItemAdder
{
	/// <summary>Add an item to the user's active shopping list.</summary>
	/// <param name="request">Item name + optional quantity / unit.</param>
	/// <param name="cancellationToken">Cancellation propagated to the call.</param>
	/// <returns>True on success, false if the upstream rejected the request.</returns>
	Task<bool> AddAsync(AddShoppingItemRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Consumer-flavored shape of an add-shopping-item request.</summary>
/// <param name="Name">Ingredient name.</param>
/// <param name="Quantity">Optional quantity; defaults applied upstream when null.</param>
/// <param name="Unit">Optional unit (g, ml, etc.).</param>
public sealed record AddShoppingItemRequest(string Name, decimal? Quantity, string? Unit);
