namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Persisted snapshot of aggregate state at a specific version.
/// </summary>
public sealed class StoredSnapshot
{
    public Guid AggregateId { get; set; }

    public string State { get; set; } = default!;

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }
}
