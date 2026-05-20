using System.Text.Json;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public static partial class JsonLdRecipeParser
#pragma warning restore SA1601
{
	private static List<ExtractedIngredientDto> ReadIngredients(JsonElement recipe)
	{
		List<ExtractedIngredientDto> list = [];
		if (!recipe.TryGetProperty("recipeIngredient", out var ingredients) || ingredients.ValueKind != JsonValueKind.Array)
		{
			return list;
		}

		foreach (var item in ingredients.EnumerateArray())
		{
			var text = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
			if (string.IsNullOrWhiteSpace(text)) continue;

			list.Add(new ExtractedIngredientDto(text.Trim(), null, null));
		}

		return list;
	}

	private static List<ExtractedStepDto> ReadSteps(JsonElement recipe)
	{
		if (!recipe.TryGetProperty("recipeInstructions", out var instructions)) return [];

		List<string> flat = [];
		FlattenInstructions(instructions, flat);

		return flat
			.Select((text, index) => new ExtractedStepDto(index + 1, text.Trim()))
			.ToList();
	}

	private static void FlattenInstructions(JsonElement element, List<string> acc)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.String:
				var value = element.GetString();
				if (!string.IsNullOrWhiteSpace(value)) acc.Add(value);
				break;
			case JsonValueKind.Array:
				foreach (var child in element.EnumerateArray())
				{
					FlattenInstructions(child, acc);
				}

				break;
			case JsonValueKind.Object:
				// HowToStep has a text property; HowToSection has itemListElement.
				if (element.TryGetProperty("text", out var text))
				{
					FlattenInstructions(text, acc);
				}
				else if (element.TryGetProperty("itemListElement", out var list))
				{
					FlattenInstructions(list, acc);
				}

				break;
			default:
				break;
		}
	}

	private static string? ReadImageUrl(JsonElement recipe)
	{
		if (!recipe.TryGetProperty("image", out var image)) return null;

		return image.ValueKind switch
		{
			JsonValueKind.String => image.GetString(),
			JsonValueKind.Array => image.EnumerateArray().Select(element => ReadImageValue(element)).FirstOrDefault(value => value is not null),
			JsonValueKind.Object => ReadImageValue(image),
			_ => null,
		};
	}

	private static string? ReadImageValue(JsonElement element) => element.ValueKind switch
	{
		JsonValueKind.String => element.GetString(),
		JsonValueKind.Object when element.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String => url.GetString(),
		_ => null,
	};
}
