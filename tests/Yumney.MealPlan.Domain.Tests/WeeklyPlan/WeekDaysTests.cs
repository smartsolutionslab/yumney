using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class WeekDaysTests
{
	[Fact]
	public void MondayToSunday_HasSevenEntries()
	{
		WeekDays.MondayToSunday.Should().HaveCount(7);
	}

	[Fact]
	public void MondayToSunday_StartsOnMondayEndsOnSunday()
	{
		WeekDays.MondayToSunday[0].Should().Be(DayOfWeek.Monday);
		WeekDays.MondayToSunday[^1].Should().Be(DayOfWeek.Sunday);
	}

	[Fact]
	public void MondayToSunday_IsInIsoOrder()
	{
		WeekDays.MondayToSunday.Should().Equal(
			DayOfWeek.Monday,
			DayOfWeek.Tuesday,
			DayOfWeek.Wednesday,
			DayOfWeek.Thursday,
			DayOfWeek.Friday,
			DayOfWeek.Saturday,
			DayOfWeek.Sunday);
	}
}
