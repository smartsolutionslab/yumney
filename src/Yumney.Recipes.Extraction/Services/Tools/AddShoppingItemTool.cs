using System.ComponentModel;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing "add to shopping list" to the chat LLM. Closes
/// the add-via-chat half of US-328.
/// </summary>
/// <param name="adder">Consumer-defined add contract.</param>
public sealed class AddShoppingItemTool(IShoppingListItemAdder adder)
{
	/// <summary>Add an item to the user's shopping list.</summary>
	/// <param name="name">Item name (e.g. "milk", "potatoes").</param>
	/// <param name="quantity">Optional numeric quantity. Default 1 if omitted.</param>
	/// <param name="unit">Optional unit ("kg", "g", "ml", "L", etc.). Omit for count-based items.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Confirmation message for the LLM to weave into its reply.</returns>
	[KernelFunction("add_to_shopping_list")]
	[Description("Add an item to the user's shopping list. Use for 'add milk', 'add 2kg potatoes', 'put eggs on the list'. The shopping list will deduplicate / sum quantities for items already present.")]
	public async Task<string> AddAsync(
		[Description("Item name, e.g. 'milk' or 'potatoes'")] string name,
		[Description("Optional numeric quantity, e.g. 2 or 500. Omit for count-based items defaulting to 1.")] decimal? quantity = null,
		[Description("Optional unit, e.g. 'kg', 'g', 'ml', 'L'. Omit for count-based items.")] string? unit = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(name)) return "Item name is required.";

		var request = new AddShoppingItemRequest(name.Trim(), quantity, unit);
		var success = await adder.AddAsync(request, cancellationToken);
		if (!success) return $"Couldn't add {name} — please try again.";

		var qtyText = quantity.HasValue ? $"{quantity}{(string.IsNullOrEmpty(unit) ? string.Empty : unit)} " : string.Empty;
		return $"Added {qtyText}{name} to your shopping list.";
	}
}
