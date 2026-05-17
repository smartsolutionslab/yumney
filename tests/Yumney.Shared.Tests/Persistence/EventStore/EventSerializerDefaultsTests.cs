using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class EventSerializerDefaultsTests
{
	private enum Side
	{
		Buy,
		Sell,
	}

	private sealed record Payload(string OwnerName, int Amount, Side Direction);

	[Fact]
	public void Options_UsesCamelCasePropertyNames()
	{
		var options = EventSerializerDefaults.Options();

		var json = JsonSerializer.Serialize(new Payload("Alice", 5, Side.Buy), options);

		json.Should().Contain("\"ownerName\":\"Alice\"");
		json.Should().Contain("\"amount\":5");
	}

	[Fact]
	public void Options_SerialisesEnumsAsStrings()
	{
		var options = EventSerializerDefaults.Options();

		var json = JsonSerializer.Serialize(new Payload("Alice", 5, Side.Sell), options);

		json.Should().Contain("\"direction\":\"Sell\"");
	}

	[Fact]
	public void Options_RegistersJsonStringEnumConverter()
	{
		var options = EventSerializerDefaults.Options();

		options.Converters.Should().ContainSingle().Which.Should().BeOfType<JsonStringEnumConverter>();
	}
}
