using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

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
		var availableTask = balanceProvider.GetAvailableIngredientsAsync(currentUser.UserId, cancellationToken);
		await Task.WhenAll(recipesTask, availableTask);

		var available = availableTask.Result;
		var matches = new List<RankedMatch>();

		foreach (var recipe in recipesTask.Result)
		{
			var missing = new List<string>();
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

			matches.Add(new RankedMatch(
				new CookableRecipeDto(
					RecipeIdentifier: recipe.Id.Value,
					Title: recipe.Title.Value,
					ImageUrl: recipe.ImageUrl?.Value,
					Servings: recipe.Servings?.Value,
					PrepTimeMinutes: recipe.Timing?.Preparation?.Value,
					CookTimeMinutes: recipe.Timing?.Cooking?.Value,
					Difficulty: recipe.Difficulty?.Value,
					IngredientCount: recipe.Ingredients.Count,
					Tier: tier,
					MissingIngredients: missing),
				urgentCount));
		}

		IReadOnlyList<CookableRecipeDto> ranked = [.. matches
			.OrderBy(m => m.Dto.Tier == CookableRecipeMatchTier.Full ? 0 : 1)
			.ThenByDescending(m => m.UrgentIngredientCount)
			.ThenBy(m => m.Dto.MissingIngredients.Count)
			.ThenBy(m => m.Dto.Title, StringComparer.OrdinalIgnoreCase)
			.Select(m => m.Dto)];

		return Result<IReadOnlyList<CookableRecipeDto>>.Success(ranked);
	}

	private sealed record RankedMatch(CookableRecipeDto Dto, int UrgentIngredientCount);
}
