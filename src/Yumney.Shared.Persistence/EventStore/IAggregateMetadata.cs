namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public interface IAggregateMetadata
{
	Guid AggregateId { get; set; }
}
