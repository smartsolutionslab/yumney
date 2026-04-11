namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

/// <summary>
/// Repository for weekly meal plans.
/// </summary>
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
    /// Get a tracked plan for updates. Throws if not found.
    /// </summary>
    /// <param name="owner">The owner identifier.</param>
    /// <param name="week">The week identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tracked weekly plan.</returns>
    Task<WeeklyPlan> GetByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new weekly plan.
    /// </summary>
    /// <param name="plan">The plan to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddAsync(WeeklyPlan plan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to a tracked plan.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
