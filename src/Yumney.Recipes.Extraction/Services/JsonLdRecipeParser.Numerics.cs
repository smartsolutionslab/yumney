using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1311
#pragma warning disable SA1601
public static partial class JsonLdRecipeParser
#pragma warning restore SA1601
{
	private static readonly Regex isoDurationPattern = IsoDuration();

	private static int? ReadServings(JsonElement recipe)
	{
		if (!recipe.TryGetProperty("recipeYield", out var yield)) return null;

		return yield.ValueKind switch
		{
			JsonValueKind.Number when yield.TryGetInt32(out var i) => i,
			JsonValueKind.String => ParseLeadingInt(yield.GetString()),
			JsonValueKind.Array => yield.EnumerateArray().Select(element => ReadServingsValue(element)).FirstOrDefault(value => value is not null),
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

	[GeneratedRegex("""^PT(?:(?<h>\d+)H)?(?:(?<m>\d+)M)?""", RegexOptions.IgnoreCase)]
	private static partial Regex IsoDuration();
}
