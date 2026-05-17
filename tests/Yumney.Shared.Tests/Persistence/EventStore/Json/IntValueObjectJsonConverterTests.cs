using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.Json;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore.Json;

public class IntValueObjectJsonConverterTests
{
	public sealed record Step : IValueObject<int>
	{
		public int Value { get; }

		private Step(int value) => Value = value;

		public static Step From(int value) => new(value);
	}

	[Fact]
	public void Write_SerializesValueAsJsonNumber()
	{
		var options = OptionsWithConverter();

		var json = JsonSerializer.Serialize(new Payload(Step.From(42)), options);

		json.Should().Contain("\"Step\":42");
	}

	[Fact]
	public void Read_DeserializesJsonNumberThroughFactory()
	{
		var options = OptionsWithConverter();

		var payload = JsonSerializer.Deserialize<Payload>("{\"Step\":99}", options);

		payload!.Step.Value.Should().Be(99);
	}

	private static JsonSerializerOptions OptionsWithConverter()
	{
		var options = new JsonSerializerOptions();
		options.Converters.Add(new IntValueObjectJsonConverter<Step>(Step.From));
		return options;
	}

	private sealed record Payload(Step Step);
}
