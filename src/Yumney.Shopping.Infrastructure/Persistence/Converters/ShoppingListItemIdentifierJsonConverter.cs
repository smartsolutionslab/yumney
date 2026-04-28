using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ShoppingListItemIdentifierJsonConverter : JsonConverter<ShoppingListItemIdentifier>
{
	public override ShoppingListItemIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		ShoppingListItemIdentifier.From(reader.GetGuid());

	public override void Write(Utf8JsonWriter writer, ShoppingListItemIdentifier value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
