using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class WeekIdentifierJsonConverter : JsonConverter<WeekIdentifier>
{
	public override WeekIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		WeekIdentifier.From(reader.GetString()!);

	public override void Write(Utf8JsonWriter writer, WeekIdentifier value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
