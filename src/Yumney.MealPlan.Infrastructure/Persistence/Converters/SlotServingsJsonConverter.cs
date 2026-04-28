using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class SlotServingsJsonConverter : JsonConverter<SlotServings>
{
	public override SlotServings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		SlotServings.From(reader.GetInt32());

	public override void Write(Utf8JsonWriter writer, SlotServings value, JsonSerializerOptions options) =>
		writer.WriteNumberValue(value.Value);
}
