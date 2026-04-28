using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class SlotRecipeTitleJsonConverter : JsonConverter<SlotRecipeTitle>
{
	public override SlotRecipeTitle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		SlotRecipeTitle.From(reader.GetString()!);

	public override void Write(Utf8JsonWriter writer, SlotRecipeTitle value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
