namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public interface IWeeklyPlanRepository
{
	/// <summary>
	/// Find a plan by owner and week. Returns null if none exists.
	/// </summary>
	/// <param name="owner">The owner identifier.</param>
	/// <param name="week">The week identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The weekly plan, or null.</returns>
	Task<WeeklyPlan?> FindByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default);

	/// <summary>
	/// Find a tracked plan for updates. Returns null if none exists.
	/// </summary>
	/// <param name="owner">The owner identifier.</param>
	/// <param name="week">The week identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The tracked weekly plan, or null.</returns>
	Task<WeeklyPlan?> FindForUpdateAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get a tracked plan for updates. Throws if not found.
	/// </summary>
	/// <param name="owner">The owner identifier.</param>
	/// <param name="week">The week identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The tracked weekly plan.</returns>
	Task<WeeklyPlan> GetByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default);

	Task AddAsync(WeeklyPlan plan, CancellationToken cancellationToken = default);
}
