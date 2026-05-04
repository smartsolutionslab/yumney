using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class NotesTests
{
	[Fact]
	public void From_ValidValue_CreatesInstance()
	{
		Notes.From("Used less salt next time").Value.Should().Be("Used less salt next time");
	}

	[Fact]
	public void From_PreservesLineBreaks()
	{
		Notes.From("Line one\nLine two").Value.Should().Be("Line one\nLine two");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => Notes.From(value!);
		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_TooLong_ThrowsGuardException()
	{
		var oversized = new string('a', Notes.MaxLength + 1);
		var act = () => Notes.From(oversized);
		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void FromNullable_NullValue_ReturnsNull()
	{
		Notes.FromNullable(null).Should().BeNull();
	}

	[Fact]
	public void FromNullable_ValidValue_ReturnsInstance()
	{
		Notes.FromNullable("hi")!.Value.Should().Be("hi");
	}
}
