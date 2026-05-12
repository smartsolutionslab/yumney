using System.ComponentModel;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing single-recipe lookup to the chat LLM. Wraps
/// <see cref="GetRecipeByIdQuery"/>. The model typically calls this after
/// <c>search_recipes</c> when the user asks for ingredients or instructions
/// of a specific result.
/// </summary>
/// <param name="handler">Query handler that fetches the recipe by identifier.</param>
/// <param name="context">Per-request collector for downstream suggestion / action emission.</param>
public sealed class GetRecipeTool(IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>> handler, ChatToolContext context)
{
	/// <summary>Fetch a single recipe's details and append a context match.</summary>
	/// <param name="recipeIdentifier">Recipe GUID returned by a prior tool call.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>The recipe detail or null if not found / invalid identifier.</returns>
	[KernelFunction("get_recipe")]
	[Description("Fetch the full details (ingredients, steps, timings, servings) of one recipe by its identifier. Use after search_recipes when the user wants to know what's in a specific recipe or how to cook it.")]
	public async Task<RecipeDetailDto?> GetAsync(
		[Description("Recipe identifier (GUID) returned by search_recipes or get_cookable_recipes")] string recipeIdentifier,
		CancellationToken cancellationToken = default)
	{
		var recipe = RecipeIdentifier.FromNullable(recipeIdentifier);

		if (recipe is null) return null;

		var result = await handler.HandleAsync(new GetRecipeByIdQuery(recipe), cancellationToken);
		if (result.IsFailure) return null;

		var detail = result.Value;
		context.AppendRecipeMatch(detail.Identifier, detail.Title);
		return detail;
	}
}
