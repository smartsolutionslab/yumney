namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

/// <summary>
/// Marker for aggregate-metadata rows whose ownership row uses the
/// keycloak-user-id string. Together with <see cref="IStoredEvent"/>, this
/// is the contract <see cref="EventSourcedAggregateDraining"/> needs to
/// purge a user's data across an event-sourced module.
/// </summary>
public interface IOwnerScopedAggregateMetadata : IAggregateMetadata
{
	string OwnerId { get; set; }
}
