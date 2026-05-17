using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class TimingInfoTests
{
	[Fact]
	public void Of_BothComponents_StampsBoth()
	{
		var prep = PreparationTime.From(10);
		var cook = CookingTime.From(20);

		var timing = TimingInfo.Of(prep, cook);

		timing.Preparation.Should().Be(prep);
		timing.Cooking.Should().Be(cook);
		timing.TotalMinutes.Should().Be(30);
	}

	[Fact]
	public void Of_PreparationOnly_LeavesCookingNull()
	{
		var prep = PreparationTime.From(15);

		var timing = TimingInfo.Of(prep, cooking: null);

		timing.Preparation.Should().Be(prep);
		timing.Cooking.Should().BeNull();
		timing.TotalMinutes.Should().Be(15);
	}

	[Fact]
	public void Of_CookingOnly_LeavesPreparationNull()
	{
		var cook = CookingTime.From(25);

		var timing = TimingInfo.Of(preparation: null, cook);

		timing.Preparation.Should().BeNull();
		timing.Cooking.Should().Be(cook);
		timing.TotalMinutes.Should().Be(25);
	}

	[Fact]
	public void Of_BothNull_StampsNullAndTotalMinutesIsNull()
	{
		var timing = TimingInfo.Of(preparation: null, cooking: null);

		timing.Preparation.Should().BeNull();
		timing.Cooking.Should().BeNull();
		timing.TotalMinutes.Should().BeNull();
	}

	[Fact]
	public void FromNullable_BothNull_ReturnsNull()
	{
		// FromNullable collapses the all-null case to null so the caller
		// doesn't need to special-case it. Distinguishes from Of (above),
		// which produces a TimingInfo with both null fields and null total.
		TimingInfo.FromNullable(preparation: null, cooking: null).Should().BeNull();
	}

	[Fact]
	public void FromNullable_PreparationOnly_ReturnsTimingInfo()
	{
		var prep = PreparationTime.From(10);

		var timing = TimingInfo.FromNullable(prep, cooking: null);

		timing.Should().NotBeNull();
		timing!.Preparation.Should().Be(prep);
		timing.Cooking.Should().BeNull();
	}

	[Fact]
	public void FromNullable_CookingOnly_ReturnsTimingInfo()
	{
		var cook = CookingTime.From(20);

		var timing = TimingInfo.FromNullable(preparation: null, cook);

		timing.Should().NotBeNull();
		timing!.Cooking.Should().Be(cook);
	}

	[Fact]
	public void Equality_SameComponents_AreEqual()
	{
		var prep = PreparationTime.From(10);
		var cook = CookingTime.From(20);

		var a = TimingInfo.Of(prep, cook);
		var b = TimingInfo.Of(prep, cook);

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
