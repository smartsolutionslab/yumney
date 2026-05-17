using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.UserActivity;

public class ActivityLimitTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(100)]
	public void From_PositiveValue_IsAccepted(int value)
	{
		var limit = ActivityLimit.From(value);

		limit.Value.Should().Be(value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void From_NonPositive_Throws(int value)
	{
		var act = () => ActivityLimit.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Default_HasDefaultValueOfFive()
	{
		ActivityLimit.Default().Value.Should().Be(ActivityLimit.DefaultValue);
		ActivityLimit.DefaultValue.Should().Be(5);
	}

	[Fact]
	public void ToString_ReturnsInvariantNumericString()
	{
		var limit = ActivityLimit.From(42);

		limit.ToString().Should().Be("42");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = ActivityLimit.From(3);
		var b = ActivityLimit.From(3);

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
