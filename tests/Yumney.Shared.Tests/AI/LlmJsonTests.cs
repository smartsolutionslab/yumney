using FluentAssertions;
using Microsoft.Extensions.AI;
using SmartSolutionsLab.Yumney.Shared.AI;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.AI;

public class LlmJsonTests
{
	[Fact]
	public void Deserialize_CamelCaseJson_MapsToRecord()
	{
		var parsed = LlmJson.Deserialize<Sample>("""{"title":"Soup","servings":4}""");

		parsed.Should().Be(new Sample("Soup", 4));
	}

	[Fact]
	public void Deserialize_JsonInMarkdownFence_MapsToRecord()
	{
		var parsed = LlmJson.Deserialize<Sample>("```json\n{\"title\":\"Soup\"}\n```");

		parsed!.Title.Should().Be("Soup");
	}

	[Fact]
	public void OptionsFor_SetsJsonSchemaResponseFormat()
	{
		var options = LlmJson.OptionsFor<Sample>();

		options.ResponseFormat.Should().BeOfType<ChatResponseFormatJson>().Which.Schema.Should().NotBeNull();
	}

	[Fact]
	public void OptionsFor_SchemaAdvertisesPlainIntegers()
	{
		var format = (ChatResponseFormatJson)LlmJson.OptionsFor<Sample>().ResponseFormat!;

		var servings = format.Schema!.Value.GetProperty("properties").GetProperty("servings").GetProperty("type");
		servings.EnumerateArray().Select(type => type.GetString()).Should().BeEquivalentTo(["integer", "null"]);
	}

	[Fact]
	public void OptionsFor_ReturnsFreshInstancePerCall()
	{
		LlmJson.OptionsFor<Sample>().Should().NotBeSameAs(LlmJson.OptionsFor<Sample>());
	}

	public sealed record Sample(string Title, int? Servings = null);
}
