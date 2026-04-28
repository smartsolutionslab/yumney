namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Materialized read model for one (owner, week) combination.
/// Tracks per-week metadata that doesn't belong on individual slots.
/// </summary>
public sealed class MealPlanWeekReadItem
{
	public string OwnerId { get; set; } = default!;

	public string Week { get; set; } = default!;

	public bool IsExtendedMode { get; set; }

	public DateTime LastUpdated { get; set; }
}
