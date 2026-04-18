using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class EnumExtensionsTests
{
	private enum TestSort
	{
		Name,
		Date,
	}

	[Fact]
	public void ParseNullable_ValidValue_ReturnsParsed()
	{
		var result = default(TestSort).ParseNullable("Name");

		result.Should().Be(TestSort.Name);
	}

	[Fact]
	public void ParseNullable_CaseInsensitive_ReturnsParsed()
	{
		var result = default(TestSort).ParseNullable("date");

		result.Should().Be(TestSort.Date);
	}

	[Fact]
	public void ParseNullable_InvalidValue_ReturnsNull()
	{
		var result = default(TestSort).ParseNullable("Invalid");

		result.Should().BeNull();
	}

	[Fact]
	public void ParseNullable_Null_ReturnsNull()
	{
		var result = default(TestSort).ParseNullable(null);

		result.Should().BeNull();
	}

	[Fact]
	public void ParseNullable_Empty_ReturnsNull()
	{
		var result = default(TestSort).ParseNullable(string.Empty);

		result.Should().BeNull();
	}

	[Fact]
	public void ParseNullable_Whitespace_ReturnsNull()
	{
		var result = default(TestSort).ParseNullable("   ");

		result.Should().BeNull();
	}
}
