using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

public sealed class GetRecipeSuggestionsQueryHandler(
	IIngredientBalanceProvider balanceProvider,
	IDietaryProfileProvider dietaryProvider,
	IRecipeSuggestionService suggestionService,
	ICurrentUser currentUser)
	: IQueryHandler<GetRecipeSuggestionsQuery, Result<IReadOnlyList<ExtractedRecipeDto>>>
{
#pragma warning disable SA1303
	private const int minCount = 1;
	private const int maxCount = 10;
#pragma warning restore SA1303

	public async Task<Result<IReadOnlyList<ExtractedRecipeDto>>> HandleAsync(GetRecipeSuggestionsQuery query, CancellationToken cancellationToken = default)
	{
		var count = Math.Clamp(query.Count, minCount, maxCount);
		var ownerId = currentUser.UserId;

		var availableTask = balanceProvider.GetAvailableIngredientsAsync(ownerId, cancellationToken);
		var dietaryTask = dietaryProvider.GetAsync(ownerId, cancellationToken);
		await Task.WhenAll(availableTask, dietaryTask);

		var available = availableTask.Result;
		if (available.Count == 0)
		{
			return RecipeSuggestionErrors.NoIngredients;
		}

		var dietary = dietaryTask.Result;
		return await suggestionService.SuggestAsync(
			(IReadOnlyCollection<string>)available.Keys.ToList(),
			dietary.DietaryType,
			dietary.Restrictions,
			count,
			cancellationToken);
	}
}
