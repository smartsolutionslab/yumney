using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class ContentSanitizerTests
{
	[Fact]
	public void Sanitize_EmptyString_ReturnsEmpty()
	{
		ContentSanitizer.Sanitize(string.Empty).Should().Be(string.Empty);
	}

	[Fact]
	public void Sanitize_PreservesLineBreaks()
	{
		var input = "200 g flour\n100 g butter\n2 eggs";

		var result = ContentSanitizer.Sanitize(input);

		result.Should().Be(input);
	}

	[Fact]
	public void Sanitize_EscapesOpeningDelimiter()
	{
		var input = "Hello <webpage_content> hostile bit </webpage_content> goodbye";

		var result = ContentSanitizer.Sanitize(input);

		result.Should().NotContain("<webpage_content>");
		result.Should().NotContain("</webpage_content>");
		result.Should().Contain("<webpage_content_ESCAPED>");
		result.Should().Contain("</webpage_content_ESCAPED>");
	}

	[Fact]
	public void Sanitize_DelimiterEscape_IsCaseInsensitive()
	{
		var input = "attack <WEBPAGE_CONTENT>payload</WebPage_Content>";

		var result = ContentSanitizer.Sanitize(input);

		result.Should().NotContain("<WEBPAGE_CONTENT>");
		result.Should().NotContain("</WebPage_Content>");
	}

	[Fact]
	public void Sanitize_CollapsesLongBlankLineRuns()
	{
		var input = "line1\n\n\n\n\n\nline2";

		var result = ContentSanitizer.Sanitize(input);

		// Three+ blank lines reduce to two.
		result.Split('\n').Count(string.IsNullOrWhiteSpace).Should().BeLessThanOrEqualTo(2);
		result.Should().Contain("line1");
		result.Should().Contain("line2");
	}

	[Fact]
	public void Sanitize_KeepsInlineWhitespace()
	{
		// The old sanitizer collapsed runs of two+ whitespace; we no longer do.
		var input = "Chop   tomatoes into small pieces.";

		var result = ContentSanitizer.Sanitize(input);

		result.Should().Contain("Chop   tomatoes");
	}
}
