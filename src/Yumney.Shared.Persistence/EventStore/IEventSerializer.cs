using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public interface IEventSerializer
{
	string Serialize(IDomainEvent @event);

	IDomainEvent? Deserialize(string eventType, string eventData);
}
