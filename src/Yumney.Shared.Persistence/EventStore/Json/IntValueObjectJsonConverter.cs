using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.Json;

/// <summary>
/// JSON converter for any int-backed value object that exposes a static factory.
/// Pass the factory delegate (typically <c>T.From</c>) at registration.
/// </summary>
/// <typeparam name="T">The value object type.</typeparam>
public sealed class IntValueObjectJsonConverter<T>(Func<int, T> factory) : JsonConverter<T>
	where T : class, IValueObject<int>
{
	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		factory(reader.GetInt32());

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
		writer.WriteNumberValue(value.Value);
}
