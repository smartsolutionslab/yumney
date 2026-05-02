using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests;

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
