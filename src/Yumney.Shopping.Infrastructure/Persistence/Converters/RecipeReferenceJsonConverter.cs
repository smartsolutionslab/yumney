using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class RecipeReferenceJsonConverter : JsonConverter<RecipeReference>
{
	public override RecipeReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		RecipeReference.From(reader.GetGuid());

	public override void Write(Utf8JsonWriter writer, RecipeReference value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
