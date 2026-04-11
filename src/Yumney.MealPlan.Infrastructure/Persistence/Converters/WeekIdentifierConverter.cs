using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed partial class WeekIdentifierConverter()
    : ValueConverter<WeekIdentifier, string>(
        v => v.Value,
        v => ParseWeekIdentifier(v))
{
    private static WeekIdentifier ParseWeekIdentifier(string value)
    {
        var match = WeekPattern().Match(value);
        return WeekIdentifier.From(
            int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    [GeneratedRegex(@"^(\d{4})-W(\d{2})$")]
    private static partial Regex WeekPattern();
}
