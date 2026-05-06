using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

public sealed class GetCookableRecipesQueryHandler(
	IRecipeRepository recipes,
	IIngredientBalanceProvider balanceProvider,
	ICurrentUser currentUser)
	: IQueryHandler<GetCookableRecipesQuery, Result<PagedResult<CookableRecipeDto>>>
{
#pragma warning disable SA1303
	private const int maxMissingForNearMatch = 2;
#pragma warning restore SA1303

	public async Task<Result<PagedResult<CookableRecipeDto>>> HandleAsync(
		GetCookableRecipesQuery query,
		CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();

		var allRecipes = await recipes.GetAllByOwnerWithIngredientsAsync(owner, cancellationToken);
		var availableIngredients = await balanceProvider.GetAvailableIngredientsAsync(cancellationToken);

		IReadOnlyList<CookableRecipeDto> ranked = [.. allRecipes
			.Select(recipe => TryRank(recipe, availableIngredients, query.FullMatchOnly))
			.OfType<RankedMatch>()
			.OrderBy(match => match.Dto.Tier == CookableRecipeMatchTier.Full ? 0 : 1)
			.ThenByDescending(match => match.UrgentIngredientCount)
			.ThenBy(match => match.Dto.MissingIngredients.Count)
			.ThenBy(match => match.Dto.Title, StringComparer.OrdinalIgnoreCase)
			.Select(match => match.Dto)];

		IReadOnlyList<CookableRecipeDto> page = [.. ranked
			.Skip(query.Paging.Skip)
			.Take(query.Paging.PageSize.Value)];

		return page.AsPagedResult(ItemCount.From(ranked.Count), query.Paging);
	}

	private static RankedMatch? TryRank(Recipe recipe, IReadOnlyDictionary<string, Freshness> available, bool fullMatchOnly)
	{
		List<string> missing = [];
		var urgentCount = 0;

		foreach (var ingredient in recipe.Ingredients)
		{
			var name = ingredient.Name.Value.Trim();
			if (!available.TryGetValue(name, out var freshness))
			{
				missing.Add(ingredient.Name.Value);
				continue;
			}

			if (freshness.IsUrgent()) urgentCount++;
		}

		var tier = missing.Count == 0 ? CookableRecipeMatchTier.Full : CookableRecipeMatchTier.Near;

		if (tier == CookableRecipeMatchTier.Near && missing.Count > maxMissingForNearMatch) return null;
		if (fullMatchOnly && tier != CookableRecipeMatchTier.Full) return null;

		return new RankedMatch(recipe.ToCookableDto(tier, missing), urgentCount);
	}

	private sealed record RankedMatch(CookableRecipeDto Dto, int UrgentIngredientCount);
}
