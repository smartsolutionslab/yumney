namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

/// <summary>
/// Tracks aggregate identity and ownership. One row per (owner, week) — i.e. one weekly plan.
/// </summary>
public sealed class AggregateMetadata
{
	public Guid AggregateId { get; set; }

	public string OwnerId { get; set; } = default!;

	public string Week { get; set; } = default!;
}
