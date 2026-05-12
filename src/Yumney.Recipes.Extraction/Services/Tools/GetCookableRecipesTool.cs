using System.ComponentModel;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

#pragma warning disable SA1303

/// <summary>
/// SK kernel function exposing the "What can I cook now?" surface to the chat
/// LLM. Wraps <see cref="GetCookableRecipesQuery"/>, which already crosses
/// into Shopping via <c>IIngredientBalanceProvider</c> for live freshness.
/// Marks the chat context as cookable so the chat service can offer a Start
/// Cooking action for the top result.
/// </summary>
/// <param name="handler">Query handler that ranks the user's recipes by cookability.</param>
/// <param name="context">Per-request collector for downstream suggestion / action emission.</param>
public sealed class GetCookableRecipesTool(
	IQueryHandler<GetCookableRecipesQuery, Result<PagedResult<CookableRecipeDto>>> handler,
	ChatToolContext context)
{
	private const int defaultPageSize = 5;

	/// <summary>Find recipes the user can cook with their current pantry.</summary>
	/// <param name="fullMatchOnly">If true, only fully-cookable recipes are returned.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Up to 5 cookable recipe hits.</returns>
	[KernelFunction("get_cookable_recipes")]
	[Description("Find recipes the user can cook right now (or with at most a couple of missing items) based on what's already in their pantry. Use for 'what can I cook?' / 'what can I make tonight?' / 'I have X and Y, what can I cook?' / 'was kann ich kochen?'.")]
	public async Task<IReadOnlyList<CookableRecipeChatHit>> GetCookableAsync(
		[Description("If true, only fully-cookable recipes are returned. Defaults to false (allows up to 2 missing ingredients).")] bool fullMatchOnly = false,
		CancellationToken cancellationToken = default)
	{
		var paging = PagingOptions.From(1, defaultPageSize);
		var result = await handler.HandleAsync(new GetCookableRecipesQuery(paging, fullMatchOnly), cancellationToken);
		if (result.IsFailure) return [];

		List<CookableRecipeChatHit> hits = [.. result.Value.Items.Select(item => item.ToChatHit())];

		foreach (var hit in hits)
		{
			context.AppendRecipeMatch(
				hit.Identifier,
				hit.Title,
				hit.Tier == "Full" ? "Ready to cook" : $"Missing: {string.Join(", ", hit.MissingIngredients)}");
		}

		if (hits.Count > 0)
		{
			context.MarkCookableQuery();
		}

		return hits;
	}
}
