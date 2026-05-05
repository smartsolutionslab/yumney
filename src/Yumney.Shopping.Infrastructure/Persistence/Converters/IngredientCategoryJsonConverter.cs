using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class IngredientCategoryJsonConverter : JsonConverter<IngredientCategory>
{
	public override IngredientCategory? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();
		return value is not null ? IngredientCategory.From(value) : null;
	}

	public override void Write(Utf8JsonWriter writer, IngredientCategory value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
