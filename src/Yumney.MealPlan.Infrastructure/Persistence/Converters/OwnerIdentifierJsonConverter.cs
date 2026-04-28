using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class OwnerIdentifierJsonConverter : JsonConverter<OwnerIdentifier>
{
	public override OwnerIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		OwnerIdentifier.From(reader.GetString()!);

	public override void Write(Utf8JsonWriter writer, OwnerIdentifier value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
