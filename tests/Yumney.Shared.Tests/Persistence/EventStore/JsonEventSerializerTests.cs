using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class JsonEventSerializerTests
{
	private sealed record CounterIncremented(int Step) : DomainEvent;

	[Fact]
	public void Serialize_RoundTripsThroughDeserialize()
	{
		var typeMap = new Dictionary<string, Type> { [nameof(CounterIncremented)] = typeof(CounterIncremented) };
		var serializer = new JsonEventSerializer(EventSerializerDefaults.Options(), typeMap);
		var original = new CounterIncremented(7);

		var json = serializer.Serialize(original);
		var roundTripped = serializer.Deserialize(nameof(CounterIncremented), json);

		roundTripped.Should().BeOfType<CounterIncremented>().Which.Step.Should().Be(7);
	}

	[Fact]
	public void Deserialize_UnknownEventType_ReturnsNull()
	{
		var serializer = new JsonEventSerializer(EventSerializerDefaults.Options(), new Dictionary<string, Type>());

		var result = serializer.Deserialize("UnknownEvent", "{}");

		result.Should().BeNull();
	}

	[Fact]
	public void Serialize_UsesCamelCaseFromDefaults()
	{
		var serializer = new JsonEventSerializer(EventSerializerDefaults.Options(), new Dictionary<string, Type>());

		var json = serializer.Serialize(new CounterIncremented(3));

		json.Should().Contain("\"step\":3");
	}
}
