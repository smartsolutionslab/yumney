namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Consumer-defined contract for fetching the user's merged active shopping
/// list from the Shopping module. Owned by Recipes (the chat surface is the
/// consumer) per CLAUDE.md cross-module rule + ADR 0002.
/// </summary>
public interface IShoppingListLookup
{
	/// <summary>Get the merged shopping list across all active lists.</summary>
	/// <param name="includePastBought">Whether to include items already bought.</param>
	/// <param name="cancellationToken">Cancellation propagated to the call.</param>
	/// <returns>Consumer-flavored merged list, or null if the upstream call fails.</returns>
	Task<ShoppingListLookupResult?> GetMergedAsync(bool includePastBought = false, CancellationToken cancellationToken = default);
}

/// <summary>Trimmed merged-list shape returned to chat tools.</summary>
public sealed record ShoppingListLookupResult(IReadOnlyList<ShoppingListLookupItem> Items);

/// <summary>One merged item across all active shopping lists.</summary>
/// <param name="Name">Ingredient name.</param>
/// <param name="Quantity">Display quantity.</param>
/// <param name="Unit">Unit, if any.</param>
/// <param name="Category">Aisle / category label.</param>
/// <param name="IsBought">Whether the item is already checked off.</param>
public sealed record ShoppingListLookupItem(
	string Name,
	decimal Quantity,
	string? Unit,
	string Category,
	bool IsBought);
