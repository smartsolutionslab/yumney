using System.Globalization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Pure date arithmetic shared by the analytics queries on
/// <see cref="MealPlanReadModelRepository"/>. Lives in its own type so
/// the conversion logic — ISO week + day-of-week → calendar date — can
/// be unit-tested without standing up a DbContext.
/// </summary>
internal static class MealPlanPeriodMath
{
	/// <summary>
	/// SQL-side bracket on the <c>Week</c> column for a period. Returns
	/// the first and last ISO-week strings to include, padded by ±1 week
	/// because ISO weeks are not aligned to calendar months/years (a
	/// Monday-start week may carry days from the prior or following
	/// month). The in-memory date filter trims to the exact bounds.
	/// </summary>
	public static (string FirstWeek, string LastWeek) SlotWeekBounds(DateOnly periodStart, DateOnly periodEndExclusive)
	{
		var inclusiveLast = periodEndExclusive.AddDays(-1);
		var first = WeekIdentifier.FromDate(periodStart.AddDays(-7)).Value;
		var last = WeekIdentifier.FromDate(inclusiveLast.AddDays(7)).Value;
		return (first, last);
	}

	/// <summary>
	/// Translates a stored <c>Week</c> ("YYYY-Www") + <c>Day</c> (enum
	/// name like "Monday") into the calendar date for that slot.
	/// </summary>
	public static DateOnly SlotDate(string weekValue, string dayValue)
	{
		var dashIndex = weekValue.IndexOf('-', StringComparison.Ordinal);
		var year = int.Parse(weekValue[..dashIndex], CultureInfo.InvariantCulture);
		var weekNumber = int.Parse(weekValue[(dashIndex + 2)..], CultureInfo.InvariantCulture);
		var dayOfWeek = Enum.Parse<DayOfWeek>(dayValue);
		var monday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday);
		var offset = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
		return DateOnly.FromDateTime(monday.AddDays(offset));
	}
}
