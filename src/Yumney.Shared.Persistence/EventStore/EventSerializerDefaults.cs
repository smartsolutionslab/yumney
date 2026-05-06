using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public static class EventSerializerDefaults
{
	public static JsonSerializerOptions Options() => new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() },
	};
}
