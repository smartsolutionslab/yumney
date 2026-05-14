using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class SlotRecipeReferenceJsonConverter : JsonConverter<SlotRecipeReference>
{
	public override SlotRecipeReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected start of object for SlotRecipeReference.");
		}

		Guid recipeIdentifier = default;
		string title = string.Empty;

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return SlotRecipeReference.From(recipeIdentifier, title);
			}

			if (reader.TokenType != JsonTokenType.PropertyName) continue;

			var propertyName = reader.GetString();
			reader.Read();

			switch (propertyName)
			{
				case "recipeIdentifier":
					recipeIdentifier = reader.GetGuid();
					break;
				case "title":
					title = reader.GetString()!;
					break;
			}
		}

		throw new JsonException("Unexpected end of JSON when reading SlotRecipeReference.");
	}

	public override void Write(Utf8JsonWriter writer, SlotRecipeReference value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("recipeIdentifier", value.Identifier.Value);
		writer.WriteString("title", value.Title.Value);
		writer.WriteEndObject();
	}
}
