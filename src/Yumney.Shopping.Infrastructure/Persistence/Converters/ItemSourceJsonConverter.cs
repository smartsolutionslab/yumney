using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ItemSourceJsonConverter : JsonConverter<ItemSource>
{
    public override ItemSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ItemSource.From(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, ItemSource value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
