using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ItemNameJsonConverter : JsonConverter<ItemName>
{
	public override ItemName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		ItemName.From(reader.GetString()!);

	public override void Write(Utf8JsonWriter writer, ItemName value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
