using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests;

internal sealed class FakeMealPlanEventStore : IMealPlanEventStore
{
	private readonly Dictionary<(string Owner, string Week), WeeklyPlan> store = [];

	public int SaveCount { get; private set; }

	public WeeklyPlan? LastSavedPlan { get; private set; }

	public async Task<WeeklyPlan> LoadAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
		=> await FindAsync(owner, week, cancellationToken)
			?? throw new EntityNotFoundException(nameof(WeeklyPlan), $"{owner.Value}/{week.Value}");

	public Task<WeeklyPlan?> FindAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
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
