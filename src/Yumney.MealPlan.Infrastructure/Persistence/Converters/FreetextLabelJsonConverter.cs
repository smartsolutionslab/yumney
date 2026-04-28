using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class FreetextLabelJsonConverter : JsonConverter<FreetextLabel>
{
	public override FreetextLabel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		FreetextLabel.From(reader.GetString()!);

	public override void Write(Utf8JsonWriter writer, FreetextLabel value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
