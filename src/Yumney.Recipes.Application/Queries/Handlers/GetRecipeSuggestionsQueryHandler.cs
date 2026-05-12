using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

public sealed class GetRecipeSuggestionsQueryHandler(
	IIngredientBalanceProvider balanceProvider,
	IDietaryProfileProvider dietaryProvider,
	IRecipeSuggestionService suggestionService)
	: IQueryHandler<GetRecipeSuggestionsQuery, Result<IReadOnlyList<ExtractedRecipeDto>>>
{
#pragma warning disable SA1303
	private const int minCount = 1;
	private const int maxCount = 10;
#pragma warning restore SA1303

	public async Task<Result<IReadOnlyList<ExtractedRecipeDto>>> HandleAsync(GetRecipeSuggestionsQuery query, CancellationToken cancellationToken = default)
	{
		var count = Math.Clamp(query.Count, minCount, maxCount);

		var availableTask = balanceProvider.GetAvailableIngredientsAsync(cancellationToken);
		var dietaryTask = dietaryProvider.GetAsync(cancellationToken);
		await Task.WhenAll(availableTask, dietaryTask);

		var available = await availableTask;
		if (available.Count == 0)
		{
			return RecipeSuggestionErrors.NoIngredients;
		}

		var dietary = await dietaryTask;
		return await suggestionService.SuggestAsync(
			(IReadOnlyCollection<string>)available.Keys.ToList(),
			dietary.DietaryType,
			dietary.Restrictions,
			count,
			cancellationToken);
	}
}
