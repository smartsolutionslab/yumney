using System.Text.Json;
using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public sealed class JsonEventSerializer(
	JsonSerializerOptions options,
	IReadOnlyDictionary<string, Type> eventTypeMap) : IEventSerializer
{
	public string Serialize(IDomainEvent @event)
		=> JsonSerializer.Serialize(@event, @event.GetType(), options);

	public IDomainEvent? Deserialize(string eventType, string eventData)
	{
		if (!eventTypeMap.TryGetValue(eventType, out var type)) return null;
		return JsonSerializer.Deserialize(eventData, type, options) as IDomainEvent;
	}
}
