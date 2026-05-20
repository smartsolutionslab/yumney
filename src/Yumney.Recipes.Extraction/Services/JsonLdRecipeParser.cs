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
			JsonValueKind.Array => type.EnumerateArray().Any(element => IsRecipeType(element.GetString())),
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

	private static string? ReadString(JsonElement element, string property)
	{
		if (!element.TryGetProperty(property, out var node)) return null;
		if (node.ValueKind != JsonValueKind.String) return null;
		var value = node.GetString();
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}

	[GeneratedRegex("""<script[^>]+type\s*=\s*["']application/ld\+json["'][^>]*>([\s\S]*?)</script>""", RegexOptions.IgnoreCase)]
	private static partial Regex JsonLdScript();
}
