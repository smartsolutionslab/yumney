using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests;

internal static class MealPlanTestFixture
{
	public static readonly OwnerIdentifier TestOwner = OwnerIdentifier.From("user-123");
	public static readonly WeekIdentifier TestWeek = WeekIdentifier.From(2026, 15);

	public static WeeklyPlan CreatePlan() => WeeklyPlan.Create(TestOwner, TestWeek);

	public static SlotRecipeReference Recipe(string title = "Pasta") =>
		SlotRecipeReference.From(SlotRecipeIdentifier.New(), SlotRecipeTitle.From(title));

	public static SlotRecipeReference Recipe(Guid id, string title) =>
		SlotRecipeReference.From(id, title);

	public static WeeklyPlan SeedPlanWithRecipe(
		FakeMealPlanEventStore eventStore,
		DayOfWeek day = DayOfWeek.Monday,
		string title = "Pasta",
		SlotServings? servings = null)
	{
		var plan = CreatePlan();
		plan.AssignRecipe(day, Recipe(title), servings: servings);
		eventStore.Seed(plan);
		return plan;
	}

	public static ICurrentUser CreateCurrentUser()
	{
		var currentUser = Substitute.For<ICurrentUser>();
		currentUser.UserId.Returns("user-123");
		return currentUser;
	}
}

internal sealed class FakeMealPlanEventStore : IMealPlanEventStore
{
	private readonly Dictionary<(string Owner, string Week), WeeklyPlan> store = [];

	public int SaveCount { get; private set; }

	public WeeklyPlan? LastSavedPlan { get; private set; }

	public Task<WeeklyPlan?> LoadAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		store.TryGetValue((owner.Value, week.Value), out var plan);
		return Task.FromResult(plan);
	}

	public Task SaveAsync(WeeklyPlan plan, CancellationToken cancellationToken = default)
	{
		store[(plan.Owner.Value, plan.Week.Value)] = plan;
		LastSavedPlan = plan;
		SaveCount++;
		plan.MarkCommitted();
		return Task.CompletedTask;
	}

	public void Seed(WeeklyPlan plan)
	{
		plan.MarkCommitted();
		store[(plan.Owner.Value, plan.Week.Value)] = plan;
	}
}

internal sealed class FakeMealPlanReadModelRepository : IMealPlanReadModelRepository
{
	private readonly Dictionary<(string Owner, string Week), WeeklyPlan> store = [];

	public Task<WeeklyPlanDto> GetByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		if (store.TryGetValue((owner.Value, week.Value), out var plan))
		{
			return Task.FromResult(plan.ToDto(week));
		}

		var emptyPlan = WeeklyPlan.Create(owner, week);
		return Task.FromResult(emptyPlan.ToDto(week));
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
