using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

#pragma warning disable SA1601
internal sealed partial class WeekIdentifierConverter()
	: ValueConverter<WeekIdentifier, string>(
		v => v.Value,
		v => ParseWeekIdentifier(v))
{
	private static WeekIdentifier ParseWeekIdentifier(string value)
	{
		var match = WeekPattern().Match(value);
		var year = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
		var weekNumber = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

		return WeekIdentifier.From(year, weekNumber);
	}

	[GeneratedRegex(@"^(\d{4})-W(\d{2})$")]
	private static partial Regex WeekPattern();
}
