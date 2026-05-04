using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

public sealed class GetCookableRecipesQueryHandler(
	IRecipeRepository recipes,
	IIngredientBalanceProvider balanceProvider,
	ICurrentUser currentUser)
	: IQueryHandler<GetCookableRecipesQuery, Result<IReadOnlyList<CookableRecipeDto>>>
{
#pragma warning disable SA1303
	private const int maxMissingForNearMatch = 2;
#pragma warning restore SA1303

	public async Task<Result<IReadOnlyList<CookableRecipeDto>>> HandleAsync(GetCookableRecipesQuery query, CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();

		var recipesTask = recipes.GetAllByOwnerWithIngredientsAsync(owner, cancellationToken);
		var availableTask = balanceProvider.GetAvailableIngredientsAsync(cancellationToken);
		await Task.WhenAll(recipesTask, availableTask);

		var available = availableTask.Result;
		List<RankedMatch> matches = [];

		foreach (var recipe in recipesTask.Result)
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

				if (freshness is Freshness.UseSoon or Freshness.CheckIt) urgentCount++;
			}

			var tier = missing.Count == 0
				? CookableRecipeMatchTier.Full
				: CookableRecipeMatchTier.Near;

			if (tier == CookableRecipeMatchTier.Near && missing.Count > maxMissingForNearMatch) continue;
			if (query.FullMatchOnly && tier != CookableRecipeMatchTier.Full) continue;

			matches.Add(new RankedMatch(recipe.ToCookableDto(tier, missing), urgentCount));
		}

		IReadOnlyList<CookableRecipeDto> ranked = [.. matches
			.OrderBy(match => match.Dto.Tier == CookableRecipeMatchTier.Full ? 0 : 1)
			.ThenByDescending(match => match.UrgentIngredientCount)
			.ThenBy(match => match.Dto.MissingIngredients.Count)
			.ThenBy(match => match.Dto.Title, StringComparer.OrdinalIgnoreCase)
			.Select(match => match.Dto)];

		return Result<IReadOnlyList<CookableRecipeDto>>.Success(ranked);
	}

	private sealed record RankedMatch(CookableRecipeDto Dto, int UrgentIngredientCount);
}
