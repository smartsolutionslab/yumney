using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class AmountJsonConverter : JsonConverter<Amount>
{
	public override Amount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		Amount.From(reader.GetDecimal());

	public override void Write(Utf8JsonWriter writer, Amount value, JsonSerializerOptions options) =>
		writer.WriteNumberValue(value.Value);
}
