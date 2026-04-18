using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class IntentParserJsonTests
{
	[Fact]
	public void ExtractJson_PlainJson_ReturnsAsIs()
	{
		var input = """{"intent":"add_to_list","entities":{"item":"milk"},"clarification":null}""";

		var result = SemanticKernelIntentParserService.ExtractJson(input);

		result.Should().Be(input);
	}

	[Fact]
	public void ExtractJson_MarkdownJsonFence_ExtractsContent()
	{
		var input = """
            ```json
            {"intent":"search_recipe","entities":{"query":"chicken"},"clarification":null}
            ```
            """;

		var result = SemanticKernelIntentParserService.ExtractJson(input);

		result.Should().Contain("search_recipe");
		result.Should().NotContain("```");
	}

	[Fact]
	public void ExtractJson_MarkdownPlainFence_ExtractsContent()
	{
		var input = """
            ```
            {"intent":"navigate","entities":{"target":"shopping-list"},"clarification":null}
            ```
            """;

		var result = SemanticKernelIntentParserService.ExtractJson(input);

		result.Should().Contain("navigate");
		result.Should().NotContain("```");
	}

	[Fact]
	public void ExtractJson_WhitespaceAroundJson_Trims()
	{
		var input = """

            {"intent":"general_chat","entities":{},"clarification":null}

            """;

		var result = SemanticKernelIntentParserService.ExtractJson(input);

		result.Should().StartWith("{");
		result.Should().EndWith("}");
	}

	[Fact]
	public void ExtractJson_EmptyString_ReturnsEmpty()
	{
		var result = SemanticKernelIntentParserService.ExtractJson(string.Empty);

		result.Should().BeEmpty();
	}

	[Fact]
	public void ExtractJson_JsonFenceCaseInsensitive_ExtractsContent()
	{
		var input = """
            ```JSON
            {"intent":"add_to_list"}
            ```
            """;

		var result = SemanticKernelIntentParserService.ExtractJson(input);

		result.Should().Contain("add_to_list");
		result.Should().NotContain("```");
	}
}
