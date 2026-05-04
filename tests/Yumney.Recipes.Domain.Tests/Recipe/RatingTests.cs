using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RatingTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public void From_ValidValue_CreatesInstance(int value)
	{
		Rating.From(value).Value.Should().Be(value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(6)]
	[InlineData(100)]
	public void From_OutOfRange_ThrowsGuardException(int value)
	{
		var act = () => Rating.From(value);
		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void FromNullable_NullValue_ReturnsNull()
	{
		Rating.FromNullable(null).Should().BeNull();
	}

	[Fact]
	public void FromNullable_ValidValue_ReturnsInstance()
	{
		Rating.FromNullable(4)!.Value.Should().Be(4);
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		Rating.From(3).Should().Be(Rating.From(3));
	}
}
