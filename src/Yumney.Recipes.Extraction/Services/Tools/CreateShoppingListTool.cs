using System.ComponentModel;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing "create a shopping list from these recipes" to
/// the chat LLM. Cross-module via <see cref="IShoppingListCreator"/>.
/// </summary>
/// <param name="creator">Consumer-defined creator contract.</param>
public sealed class CreateShoppingListTool(IShoppingListCreator creator)
{
	/// <summary>Create a shopping list named <paramref name="title"/> from the given comma-separated recipe identifiers.</summary>
	/// <param name="title">List title (e.g. "This week's dinners").</param>
	/// <param name="recipeIdentifiers">Comma-separated list of recipe GUIDs from prior search/get calls.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Human-readable confirmation string for the LLM to weave into its reply.</returns>
	[KernelFunction("create_shopping_list_from_recipes")]
	[Description("Create a new shopping list sourced from one or more recipes. Use for 'make a shopping list for spaghetti and risotto', 'create a list for this week's dinners'. Pass comma-separated recipe identifiers from prior search_recipes / get_recipe calls.")]
	public async Task<string> CreateAsync(
		[Description("List title, e.g. 'This week's dinners'")] string title,
		[Description("Comma-separated recipe GUIDs (e.g. '8d4d…, c2f7…') from prior search_recipes calls")] string recipeIdentifiers,
		CancellationToken cancellationToken = default)
	{
		var parsed = ParseGuids(recipeIdentifiers);
		if (parsed.Count == 0) return "Need at least one valid recipe identifier — call search_recipes first.";

		var request = new CreateShoppingListRequest(
			title,
			[.. parsed.Select(id => new CreateShoppingListRecipe(id, Servings: null))]);

		var success = await creator.CreateAsync(request, cancellationToken);
		return success
			? $"Created '{title}' with {parsed.Count} recipe(s)."
			: $"Couldn't create '{title}' — please try again.";
	}

	private static List<Guid> ParseGuids(string commaSeparated)
	{
		List<Guid> result = [];
		foreach (var token in commaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (Guid.TryParse(token, out var guid)) result.Add(guid);
		}

		return result;
	}
}
