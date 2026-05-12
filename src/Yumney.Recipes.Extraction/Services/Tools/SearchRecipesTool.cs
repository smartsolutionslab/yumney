using System.ComponentModel;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

#pragma warning disable SA1303

/// <summary>
/// SK kernel function exposing recipe search to the chat LLM. Wraps the
/// existing <see cref="GetRecipesQuery"/> handler.
/// </summary>
/// <param name="handler">Query handler that runs the recipe search.</param>
/// <param name="context">Per-request collector for downstream suggestion / action emission.</param>
public sealed class SearchRecipesTool(IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>> handler,
	ChatToolContext context)
{
	private const int defaultPageSize = 10;

	/// <summary>Search the user's recipes by free text and append matches to the chat tool context.</summary>
	/// <param name="query">Free text search query.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Up to 10 recipe hits.</returns>
	[KernelFunction("search_recipes")]
	[Description("Search the user's recipe collection by free text query. Returns up to 10 recipes ordered by recency. Use when the user asks about a specific dish or ingredient theme (e.g. 'find chicken recipes', 'what pasta dishes do I have').")]
	public async Task<IReadOnlyList<RecipeChatHit>> SearchAsync(
		[Description("Free text search query — a dish name, ingredient, or theme")] string query,
		CancellationToken cancellationToken = default)
	{
		var paging = PagingOptions.From(1, defaultPageSize);
		var sorting = new SortingOptions<RecipeSortField>(RecipeSortField.Date, SortDirection.Descending);
		var search = SearchTerm.FromNullable(query);

		var result = await handler.HandleAsync(new GetRecipesQuery(paging, sorting, search), cancellationToken);
		if (result.IsFailure) return [];

		List<RecipeChatHit> hits = [.. result.Value.Items.Select(item => item.ToChatHit())];
		foreach (var hit in hits)
		{
			context.AppendRecipeMatch(hit.Identifier, hit.Title);
		}

		return hits;
	}
}
