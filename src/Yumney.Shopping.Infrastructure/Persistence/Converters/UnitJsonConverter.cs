using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class UnitJsonConverter : JsonConverter<Unit>
{
	public override Unit? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();
		return value is not null ? Unit.From(value) : null;
	}

	public override void Write(Utf8JsonWriter writer, Unit value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.Value);
}
