using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.Json;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore.Json;

public class StringValueObjectJsonConverterTests
{
	public sealed record OwnerName : IValueObject<string>
	{
		public string Value { get; }

		private OwnerName(string value) => Value = value;

		public static OwnerName From(string value) => new(value);
	}

	[Fact]
	public void Write_SerializesValueAsJsonString()
	{
		var options = OptionsWithConverter();

		var json = JsonSerializer.Serialize(new Payload(OwnerName.From("alice")), options);

		json.Should().Contain("\"Owner\":\"alice\"");
	}

	[Fact]
	public void Read_DeserializesJsonStringThroughFactory()
	{
		var options = OptionsWithConverter();

		var payload = JsonSerializer.Deserialize<Payload>("{\"Owner\":\"bob\"}", options);

		payload!.Owner.Value.Should().Be("bob");
	}

	private static JsonSerializerOptions OptionsWithConverter()
	{
		var options = new JsonSerializerOptions();
		options.Converters.Add(new StringValueObjectJsonConverter<OwnerName>(OwnerName.From));
		return options;
	}

	private sealed record Payload(OwnerName Owner);
}
