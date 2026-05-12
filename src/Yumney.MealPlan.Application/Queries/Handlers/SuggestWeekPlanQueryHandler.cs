using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

public sealed class SuggestWeekPlanQueryHandler(
	IRecipeCatalogProvider catalog,
	IMealPlanReadModelRepository readModel,
	IDietaryProfileProvider dietary,
	IWeekSuggestionService suggestionService,
	ICurrentUser currentUser)
	: IQueryHandler<SuggestWeekPlanQuery, Result<WeekSuggestionDto>>
{
#pragma warning disable SA1303
	private const int catalogPageSize = 50;
	private const int historyLookbackEntries = 30;
#pragma warning restore SA1303

	public async Task<Result<WeekSuggestionDto>> HandleAsync(
		SuggestWeekPlanQuery query,
		CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();

		var catalogTask = catalog.ListAsync(catalogPageSize, cancellationToken);
		var historyTask = readModel.SearchCookedHistoryAsync(owner, term: null, PagingOptions.From(1, historyLookbackEntries), cancellationToken);
		var dietaryTask = dietary.GetAsync(cancellationToken);

		await Task.WhenAll(catalogTask, historyTask, dietaryTask);

		var recipes = await catalogTask;
		if (recipes.Count == 0) return SuggestWeekPlanErrors.NoRecipes;

		var history = await historyTask;
		var dietaryProfile = await dietaryTask;

		var entries = await suggestionService.SuggestAsync(
			query.Week,
			recipes,
			history.Items,
			dietaryProfile,
			cancellationToken);

		if (entries.IsFailure) return entries.Error!;

		return Result<WeekSuggestionDto>.Success(new WeekSuggestionDto(query.Week.ToString(), entries.Value!));
	}
}
