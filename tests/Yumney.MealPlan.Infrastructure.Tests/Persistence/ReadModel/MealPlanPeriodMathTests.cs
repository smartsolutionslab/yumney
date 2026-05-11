using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Persistence.ReadModel;

public class MealPlanPeriodMathTests
{
	public class SlotDateTests
	{
		[Theory]
		[InlineData("2026-W20", "Monday", 2026, 5, 11)]
		[InlineData("2026-W20", "Sunday", 2026, 5, 17)]
		[InlineData("2026-W01", "Thursday", 2026, 1, 1)]
		[InlineData("2025-W01", "Monday", 2024, 12, 30)]
		public void SlotDate_ReturnsExpectedCalendarDate(
			string week,
			string day,
			int expectedYear,
			int expectedMonth,
			int expectedDay)
		{
			var date = MealPlanPeriodMath.SlotDate(week, day);

			date.Should().Be(new DateOnly(expectedYear, expectedMonth, expectedDay));
		}

		[Fact]
		public void SlotDate_FullWeekMondayToSunday_IsSevenConsecutiveDays()
		{
			var monday = MealPlanPeriodMath.SlotDate("2026-W20", "Monday");
			var days = new[]
			{
				MealPlanPeriodMath.SlotDate("2026-W20", "Tuesday"),
				MealPlanPeriodMath.SlotDate("2026-W20", "Wednesday"),
				MealPlanPeriodMath.SlotDate("2026-W20", "Thursday"),
				MealPlanPeriodMath.SlotDate("2026-W20", "Friday"),
				MealPlanPeriodMath.SlotDate("2026-W20", "Saturday"),
				MealPlanPeriodMath.SlotDate("2026-W20", "Sunday"),
			};

			for (var index = 0; index < days.Length; index++)
			{
				days[index].Should().Be(monday.AddDays(index + 1));
			}
		}

		[Fact]
		public void SlotDate_53WeekYear_HandlesW53()
		{
			// 2026 has 53 ISO weeks.
			var date = MealPlanPeriodMath.SlotDate("2026-W53", "Monday");

			date.Should().Be(new DateOnly(2026, 12, 28));
		}
	}

	public class SlotWeekBoundsTests
	{
		[Fact]
		public void SlotWeekBounds_MayMonth_BracketsBeforeAndAfter()
		{
			var start = new DateOnly(2026, 5, 1);
			var endExclusive = new DateOnly(2026, 6, 1);

			var (firstWeek, lastWeek) = MealPlanPeriodMath.SlotWeekBounds(start, endExclusive);

			// firstWeek must be at or before the ISO week containing May 1.
			string.Compare(firstWeek, "2026-W18", StringComparison.Ordinal).Should().BeLessThanOrEqualTo(0);

			// lastWeek must be at or after the ISO week containing May 31.
			string.Compare(lastWeek, "2026-W22", StringComparison.Ordinal).Should().BeGreaterThanOrEqualTo(0);
		}

		[Fact]
		public void SlotWeekBounds_JanuaryMonth_BracketsAcrossYearBoundary()
		{
			var start = new DateOnly(2026, 1, 1);
			var endExclusive = new DateOnly(2026, 2, 1);

			var (firstWeek, _) = MealPlanPeriodMath.SlotWeekBounds(start, endExclusive);

			// One week before Jan 1 lives in 2025 ISO calendar
			firstWeek.Should().StartWith("2025-W");
		}

		[Fact]
		public void SlotWeekBounds_FullYear_FirstIsPriorYear_LastIsFollowingYear()
		{
			var start = new DateOnly(2026, 1, 1);
			var endExclusive = new DateOnly(2027, 1, 1);

			var (firstWeek, lastWeek) = MealPlanPeriodMath.SlotWeekBounds(start, endExclusive);

			firstWeek.Should().StartWith("2025-W");
			lastWeek.Should().StartWith("2027-W");
		}
	}
}
