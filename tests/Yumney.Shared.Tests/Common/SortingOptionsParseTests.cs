using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class SortingOptionsParseTests
{
	private enum SortField
	{
		CreatedAt,
		Title,
		Rating,
	}

	[Fact]
	public void Parse_KnownValue_ReturnsParsedField()
	{
		var options = SortingOptions<SortField>.Parse("Title", SortDirection.Ascending, fallback: SortField.CreatedAt);

		options.SortBy.Should().Be(SortField.Title);
		options.Direction.Should().Be(SortDirection.Ascending);
	}

	[Fact]
	public void Parse_KnownValueCaseInsensitive_ReturnsParsedField()
	{
		var options = SortingOptions<SortField>.Parse("rating", SortDirection.Descending, fallback: SortField.CreatedAt);

		options.SortBy.Should().Be(SortField.Rating);
	}

	[Fact]
	public void Parse_NullValue_UsesFallback()
	{
		var options = SortingOptions<SortField>.Parse(null, SortDirection.Descending, fallback: SortField.CreatedAt);

		options.SortBy.Should().Be(SortField.CreatedAt);
		options.Direction.Should().Be(SortDirection.Descending);
	}

	[Fact]
	public void Parse_UnknownValue_UsesFallback()
	{
		var options = SortingOptions<SortField>.Parse("Unknown", SortDirection.Ascending, fallback: SortField.Rating);

		options.SortBy.Should().Be(SortField.Rating);
	}
}
