namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public interface IStoredEvent
{
	Guid Id { get; set; }

	Guid AggregateId { get; set; }

	string EventType { get; set; }

	string EventData { get; set; }

	int Version { get; set; }

	DateTime OccurredAt { get; set; }
}
