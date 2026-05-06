using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Tracks aggregate identity and ownership. One row per aggregate.
/// </summary>
public sealed class AggregateMetadata : IAggregateMetadata
{
	public Guid AggregateId { get; set; }

	public string OwnerId { get; set; } = default!;
}
