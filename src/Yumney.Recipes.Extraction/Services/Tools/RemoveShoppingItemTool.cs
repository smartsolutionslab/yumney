using System.ComponentModel;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing "remove from shopping list" to the chat LLM.
/// Closes the remove-via-chat half of US-328.
/// </summary>
/// <param name="remover">Consumer-defined remove contract.</param>
public sealed class RemoveShoppingItemTool(IShoppingListItemRemover remover)
{
	/// <summary>Remove an item from the user's shopping list.</summary>
	/// <param name="name">Item name to remove.</param>
	/// <param name="quantity">Optional quantity to remove. Omit to remove the entire entry.</param>
	/// <param name="unit">Optional unit.</param>
	/// <param name="reason">Optional removal reason ("bought", "skipped", "no_longer_needed").</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Confirmation message for the LLM.</returns>
	[KernelFunction("remove_from_shopping_list")]
	[Description("Remove an item from the user's shopping list. Use for 'remove eggs', 'I don't need milk anymore', 'cross off the bread'. Omit quantity to remove the entire entry.")]
	public async Task<string> RemoveAsync(
		[Description("Item name to remove from the list")] string name,
		[Description("Optional numeric quantity. Omit to remove the entire entry.")] decimal? quantity = null,
		[Description("Optional unit, e.g. 'kg', 'g', 'ml'.")] string? unit = null,
		[Description("Optional removal reason: 'bought', 'skipped', 'no_longer_needed'.")] string? reason = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(name)) return "Item name is required.";

		var success = await remover.RemoveAsync(new RemoveShoppingItemRequest(name.Trim(), quantity, unit, reason), cancellationToken);
		return success
			? $"Removed {name} from your shopping list."
			: $"Couldn't find {name} on your list.";
	}
}
