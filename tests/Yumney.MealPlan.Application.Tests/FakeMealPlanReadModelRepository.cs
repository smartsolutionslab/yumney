using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests;

internal sealed class FakeMealPlanReadModelRepository : IMealPlanReadModelRepository
{
	private readonly Dictionary<(string Owner, string Week), WeeklyPlan> store = [];

	public Task<WeeklyPlanDto> GetByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		if (store.TryGetValue((owner.Value, week.Value), out var plan))
		{
			return Task.FromResult(plan.ToDto());
		}

		var emptyPlan = WeeklyPlan.Create(owner, week);
		return Task.FromResult(emptyPlan.ToDto());
	}

	public Task<WeeklyPlannedRecipesDto> GetPlannedRecipesAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		if (!store.TryGetValue((owner.Value, week.Value), out var plan))
		{
			return Task.FromResult(new WeeklyPlannedRecipesDto(week.Value, []));
		}

		var recipes = plan.Slots
			.Where(slot => slot.ContentType == SlotContentType.Recipe && slot.Recipe is not null)
			.Select(slot => new PlannedRecipeDto(
				slot.Recipe!.RecipeIdentifier.Value,
				slot.Recipe.Title.Value,
				slot.Servings.Value,
				slot.Day.ToString(),
				slot.MealType.ToString()))
			.ToList();

		return Task.FromResult(new WeeklyPlannedRecipesDto(week.Value, recipes));
	}

	public Task<IReadOnlyList<MealHistoryEntryDto>> SearchCookedHistoryAsync(OwnerIdentifier owner, string? term, int limit, CancellationToken cancellationToken = default)
	{
		IEnumerable<MealHistoryEntryDto> rows = store
			.Where(kv => kv.Key.Owner == owner.Value)
			.SelectMany(kv => kv.Value.Slots
				.Where(slot => slot.State == MealState.Cooked && slot.Recipe is not null)
				.Select(slot => new MealHistoryEntryDto(
					slot.Recipe!.RecipeIdentifier.Value,
					slot.Recipe.Title.Value,
					kv.Key.Week,
					slot.Day.ToString(),
					slot.MealType.ToString())));

		if (!string.IsNullOrWhiteSpace(term))
		{
			rows = rows.Where(entry => entry.RecipeTitle.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase));
		}

		return Task.FromResult<IReadOnlyList<MealHistoryEntryDto>>(rows
			.OrderByDescending(entry => entry.Week)
			.ThenBy(entry => entry.Day)
			.ThenBy(entry => entry.MealType)
			.Take(limit)
			.ToList());
	}

	public void Seed(WeeklyPlan plan)
	{
		store[(plan.Owner.Value, plan.Week.Value)] = plan;
	}
}
