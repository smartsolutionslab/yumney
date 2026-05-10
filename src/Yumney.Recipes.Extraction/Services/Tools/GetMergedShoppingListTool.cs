using System.ComponentModel;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing the user's merged active shopping list to the
/// chat LLM. Cross-module via <see cref="IShoppingListLookup"/>.
/// </summary>
/// <param name="lookup">Consumer-defined merged-list lookup.</param>
public sealed class GetMergedShoppingListTool(IShoppingListLookup lookup)
{
	/// <summary>Get the user's merged active shopping list.</summary>
	/// <param name="includePastBought">Whether to include items already bought.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>The merged list or null if nothing on the list.</returns>
	[KernelFunction("get_merged_shopping_list")]
	[Description("Fetch the user's active shopping list, merged across all open lists. Use for 'what's on my shopping list?', 'do I need to buy anything for chicken?', 'was steht auf meiner Einkaufsliste?'. Defaults to unbought items only.")]
	public Task<ShoppingListLookupResult?> GetAsync(
		[Description("If true, also include items already bought (for review). Defaults to false.")] bool includePastBought = false,
		CancellationToken cancellationToken = default) =>
		lookup.GetMergedAsync(includePastBought, cancellationToken);
}
