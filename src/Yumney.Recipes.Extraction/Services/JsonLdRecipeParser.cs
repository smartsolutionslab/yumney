using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

/// <summary>
/// Extracts schema.org/Recipe data from JSON-LD blocks in an HTML document.
/// Most mainstream recipe sites embed this; parsing it lets us skip the LLM
/// entirely with higher accuracy.
/// </summary>
#pragma warning disable SA1311
public static partial class JsonLdRecipeParser
{
	private static readonly JsonDocumentOptions documentOptions = new()
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip,
	};

	private static readonly Regex isoDurationPattern = IsoDuration();

	/// <summary>
	/// Attempts to extract a Recipe from any application/ld+json script in the HTML.
	/// Returns null when no parseable Recipe is found. A parseable Recipe requires
	/// a non-empty name and a non-empty ingredient list; missing instructions fall
	/// back to the LLM path.
	/// </summary>
	/// <param name="html">The raw HTML of the scraped page.</param>
	/// <returns>A populated <see cref="ExtractedRecipeDto"/>, or <c>null</c> when no parseable JSON-LD Recipe is present.</returns>
	public static ExtractedRecipeDto? TryParse(string html)
	{
		foreach (var blob in ExtractJsonLdBlobs(html))
		{
			var recipe = TryParseBlob(blob);
			if (recipe is not null) return recipe;
		}

		return null;
	}

	private static IEnumerable<string> ExtractJsonLdBlobs(string html)
	{
		foreach (Match match in JsonLdScript().Matches(html))
		{
			if (match.Groups.Count < 2) continue;
			yield return match.Groups[1].Value;
		}
	}

	private static ExtractedRecipeDto? TryParseBlob(string json)
	{
		try
		{
			using var doc = JsonDocument.Parse(json, documentOptions);
			return FindRecipe(doc.RootElement);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static ExtractedRecipeDto? FindRecipe(JsonElement element)
	{
		if (element.ValueKind == JsonValueKind.Array)
		{
			foreach (var item in element.EnumerateArray())
			{
				var recipe = FindRecipe(item);
				if (recipe is not null) return recipe;
			}

			return null;
		}

		if (element.ValueKind != JsonValueKind.Object) return null;

		if (IsRecipe(element)) return MapRecipe(element);

		if (element.TryGetProperty("@graph", out var graph)) return FindRecipe(graph);
		if (element.TryGetProperty("mainEntity", out var mainEntity)) return FindRecipe(mainEntity);
		if (element.TryGetProperty("mainEntityOfPage", out var mainPage) && mainPage.ValueKind == JsonValueKind.Object)
		{
			var nested = FindRecipe(mainPage);
			if (nested is not null) return nested;
		}

		return null;
	}

	private static bool IsRecipe(JsonElement element)
	{
		if (!element.TryGetProperty("@type", out var type)) return false;

		return type.ValueKind switch
		{
			JsonValueKind.String => IsRecipeType(type.GetString()),
			JsonValueKind.Array => type.EnumerateArray().Any(t => IsRecipeType(t.GetString())),
			_ => false,
		};
	}

	private static bool IsRecipeType(string? type)
		=> type is not null && type.Equals("Recipe", StringComparison.OrdinalIgnoreCase);

	private static ExtractedRecipeDto? MapRecipe(JsonElement recipe)
	{
		var title = ReadString(recipe, "name")?.Trim();
		if (string.IsNullOrWhiteSpace(title)) return null;

		var ingredients = ReadIngredients(recipe);
		if (ingredients.Count == 0) return null;

		var steps = ReadSteps(recipe);
		if (steps.Count == 0) return null;

		return new ExtractedRecipeDto(
			Title: title,
			Ingredients: ingredients,
			Steps: steps,
			Description: ReadString(recipe, "description"),
			Language: ReadString(recipe, "inLanguage"),
			Servings: ReadServings(recipe),
			PrepTimeMinutes: ReadDurationMinutes(recipe, "prepTime"),
			CookTimeMinutes: ReadDurationMinutes(recipe, "cookTime"),
			Difficulty: null,
			ImageUrl: ReadImageUrl(recipe));
	}

	private static List<ExtractedIngredientDto> ReadIngredients(JsonElement recipe)
	{
		var list = new List<ExtractedIngredientDto>();
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

		var flat = new List<string>();
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

	private static int? ReadServings(JsonElement recipe)
	{
		if (!recipe.TryGetProperty("recipeYield", out var yield)) return null;

		return yield.ValueKind switch
		{
			JsonValueKind.Number when yield.TryGetInt32(out var i) => i,
			JsonValueKind.String => ParseLeadingInt(yield.GetString()),
			JsonValueKind.Array => yield.EnumerateArray().Select(e => ReadServingsValue(e)).FirstOrDefault(v => v is not null),
			_ => null,
		};
	}

	private static int? ReadServingsValue(JsonElement element) => element.ValueKind switch
	{
		JsonValueKind.Number when element.TryGetInt32(out var i) => i,
		JsonValueKind.String => ParseLeadingInt(element.GetString()),
		_ => null,
	};

	private static int? ParseLeadingInt(string? text)
	{
		if (string.IsNullOrWhiteSpace(text)) return null;

		var digits = new string(text.TakeWhile(char.IsDigit).ToArray());
		return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
	}

	private static int? ReadDurationMinutes(JsonElement recipe, string property)
	{
		if (!recipe.TryGetProperty(property, out var node) || node.ValueKind != JsonValueKind.String) return null;

		var raw = node.GetString();
		if (string.IsNullOrWhiteSpace(raw)) return null;

		var match = isoDurationPattern.Match(raw);
		if (!match.Success) return null;

		var hours = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture) : 0;
		var minutes = match.Groups["m"].Success ? int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture) : 0;
		var total = (hours * 60) + minutes;
		return total > 0 ? total : null;
	}

	private static string? ReadImageUrl(JsonElement recipe)
	{
		if (!recipe.TryGetProperty("image", out var image)) return null;

		return image.ValueKind switch
		{
			JsonValueKind.String => image.GetString(),
			JsonValueKind.Array => image.EnumerateArray().Select(e => ReadImageValue(e)).FirstOrDefault(v => v is not null),
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

	private static string? ReadString(JsonElement element, string property)
	{
		if (!element.TryGetProperty(property, out var node)) return null;
		if (node.ValueKind != JsonValueKind.String) return null;
		var value = node.GetString();
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}

	[GeneratedRegex("""<script[^>]+type\s*=\s*["']application/ld\+json["'][^>]*>([\s\S]*?)</script>""", RegexOptions.IgnoreCase)]
	private static partial Regex JsonLdScript();

	[GeneratedRegex("""^PT(?:(?<h>\d+)H)?(?:(?<m>\d+)M)?""", RegexOptions.IgnoreCase)]
	private static partial Regex IsoDuration();
}
