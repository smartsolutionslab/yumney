namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public interface IMealPlanEventStore
{
	Task<WeeklyPlan> LoadAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default);

	Task<WeeklyPlan?> FindAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default);

	Task SaveAsync(WeeklyPlan plan, CancellationToken cancellationToken = default);
}
