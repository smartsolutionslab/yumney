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
		var availableTask = balanceProvider.GetAvailableIngredientNamesAsync(currentUser.UserId, cancellationToken);
		await Task.WhenAll(recipesTask, availableTask);

		var available = availableTask.Result;
		var matches = new List<CookableRecipeDto>();

		foreach (var recipe in recipesTask.Result)
		{
			var missing = recipe.Ingredients
				.Where(i => !available.Contains(i.Name.Value.Trim()))
				.Select(i => i.Name.Value)
				.ToList();

			var tier = missing.Count == 0
				? CookableRecipeMatchTier.Full
				: CookableRecipeMatchTier.Near;

			if (tier == CookableRecipeMatchTier.Near && missing.Count > maxMissingForNearMatch) continue;
			if (query.FullMatchOnly && tier != CookableRecipeMatchTier.Full) continue;

			matches.Add(new CookableRecipeDto(
				RecipeIdentifier: recipe.Id.Value,
				Title: recipe.Title.Value,
				ImageUrl: recipe.ImageUrl?.Value,
				Servings: recipe.Servings?.Value,
				PrepTimeMinutes: recipe.Timing?.Preparation?.Value,
				CookTimeMinutes: recipe.Timing?.Cooking?.Value,
				Difficulty: recipe.Difficulty?.Value,
				IngredientCount: recipe.Ingredients.Count,
				Tier: tier,
				MissingIngredients: missing));
		}

		IReadOnlyList<CookableRecipeDto> ranked = [.. matches
			.OrderBy(m => m.Tier == CookableRecipeMatchTier.Full ? 0 : 1)
			.ThenBy(m => m.MissingIngredients.Count)
			.ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)];

		return Result<IReadOnlyList<CookableRecipeDto>>.Success(ranked);
	}
}
