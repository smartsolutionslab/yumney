using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ShoppingListIdentifierJsonConverter : JsonConverter<ShoppingListIdentifier>
{
	public override ShoppingListIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		ShoppingListIdentifier.From(reader.GetGuid());

	public override void Write(Utf8JsonWriter writer, ShoppingListIdentifier value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
