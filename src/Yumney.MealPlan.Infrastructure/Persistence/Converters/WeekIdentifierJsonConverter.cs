using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class WeekIdentifierJsonConverter : JsonConverter<WeekIdentifier>
{
	public override WeekIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString()!;
		var parts = value.Split("-W");
		var year = int.Parse(parts[0], CultureInfo.InvariantCulture);
		var weekNumber = int.Parse(parts[1], CultureInfo.InvariantCulture);
		return WeekIdentifier.From(year, weekNumber);
	}

	public override void Write(Utf8JsonWriter writer, WeekIdentifier value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
