using System.Globalization;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record WeekIdentifier : IValueObject<string>
{
    public int Year { get; }

    public int WeekNumber { get; }

    public string Value { get; }

    private WeekIdentifier(int year, int weekNumber)
    {
        Ensure.That(year).IsInRange(2020, 2100);
        Ensure.That(weekNumber).IsInRange(1, 53);
        Year = year;
        WeekNumber = weekNumber;
        Value = $"{year}-W{weekNumber:D2}";
    }

    public static WeekIdentifier From(int year, int weekNumber) => new(year, weekNumber);

    public static WeekIdentifier FromDate(DateOnly date)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        var week = cal.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue), CalendarWeekRule.FirstFourDayWeek, System.DayOfWeek.Monday);
        var year = date.Year;
        if (week == 1 && date.Month == 12) year++;
        if (week >= 52 && date.Month == 1) year--;

        return new WeekIdentifier(year, week);
    }

    public static WeekIdentifier Current() => FromDate(DateOnly.FromDateTime(DateTime.UtcNow));

    public static implicit operator string(WeekIdentifier obj) => obj.Value;

    public override string ToString() => Value;
}
