using System.ComponentModel;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing recipe rating to the chat LLM. Wraps
/// <see cref="RateRecipeCommand"/> — same Recipes-owned pattern as the other
/// in-process tools (no cross-module HTTP needed). Closes the chat surface
/// for US-187.
/// </summary>
/// <param name="handler">Command handler that persists the rating.</param>
public sealed class RateRecipeTool(ICommandHandler<RateRecipeCommand, Result> handler)
{
	/// <summary>Rate a recipe 1-5 stars.</summary>
	/// <param name="recipeIdentifier">Recipe GUID returned by a prior search/get.</param>
	/// <param name="rating">Integer 1-5.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Confirmation message for the LLM to weave into its reply.</returns>
	[KernelFunction("rate_recipe")]
	[Description("Rate a recipe in the user's collection 1-5 stars ('rate the carbonara 5 stars', 'I'd give that pasta a 4'). Requires a recipe identifier from a prior search_recipes / get_recipe call.")]
	public async Task<string> RateAsync(
		[Description("Recipe identifier (GUID) returned by search_recipes or get_recipe")] string recipeIdentifier,
		[Description("Star rating, 1 to 5")] int rating,
		CancellationToken cancellationToken = default)
	{
		if (!Guid.TryParse(recipeIdentifier, out var guid)) return "Invalid recipe identifier — call search_recipes first.";
		if (rating < Rating.MinValue || rating > Rating.MaxValue) return $"Rating must be between {Rating.MinValue} and {Rating.MaxValue}.";

		var command = new RateRecipeCommand(RecipeIdentifier.From(guid), Rating.From(rating));
		var result = await handler.HandleAsync(command, cancellationToken);
		return result.IsSuccess
			? $"Saved your {rating}-star rating."
			: "Couldn't save the rating — make sure the recipe is in your collection.";
	}
}
