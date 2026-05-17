using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Application.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Common;

public class LlmResponseParserTests
{
	[Fact]
	public void ExtractJson_NoFence_ReturnsTrimmedInput()
	{
		var result = LlmResponseParser.ExtractJson("  {\"a\":1}  ");

		result.Should().Be("{\"a\":1}");
	}

	[Fact]
	public void ExtractJson_JsonFence_StripsPrefixAndSuffix()
	{
		var result = LlmResponseParser.ExtractJson("```json\n{\"a\":1}\n```");

		result.Should().Be("{\"a\":1}");
	}

	[Fact]
	public void ExtractJson_JsonFence_CaseInsensitivePrefix()
	{
		var result = LlmResponseParser.ExtractJson("```JSON\n{\"a\":1}\n```");

		result.Should().Be("{\"a\":1}");
	}

	[Fact]
	public void ExtractJson_PlainFence_StripsPrefixAndSuffix()
	{
		var result = LlmResponseParser.ExtractJson("```\n{\"a\":1}\n```");

		result.Should().Be("{\"a\":1}");
	}

	[Fact]
	public void ExtractJson_OnlyOpenFence_StripsPrefixAndReturnsRemainder()
	{
		var result = LlmResponseParser.ExtractJson("```json\n{\"a\":1}");

		result.Should().Be("{\"a\":1}");
	}

	[Fact]
	public void ExtractJson_OnlyClosingFence_StripsSuffix()
	{
		var result = LlmResponseParser.ExtractJson("{\"a\":1}\n```");

		result.Should().Be("{\"a\":1}");
	}

	[Fact]
	public void ExtractJson_NoFenceJustWhitespace_ReturnsEmpty()
	{
		var result = LlmResponseParser.ExtractJson("   \n   ");

		result.Should().BeEmpty();
	}
}
