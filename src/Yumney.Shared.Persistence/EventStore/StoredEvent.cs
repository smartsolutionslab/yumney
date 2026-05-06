namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

/// <summary>
/// Persisted event row in an append-only event store. Each module maps this type
/// to its own table (e.g. <c>ShoppingEvents</c>, <c>MealPlanEvents</c>) via its
/// own <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/>.
/// </summary>
public sealed class StoredEvent : IStoredEvent
{
	public Guid Id { get; set; }

	public Guid AggregateId { get; set; }

	public string EventType { get; set; } = default!;

	public string EventData { get; set; } = default!;

	public int Version { get; set; }

	public DateTime OccurredAt { get; set; }
}
