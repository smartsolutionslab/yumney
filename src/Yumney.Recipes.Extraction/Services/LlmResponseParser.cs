namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

public static class LlmResponseParser
{
#pragma warning disable SA1303
	private const string jsonFencePrefix = "```json";
	private const string fenceMarker = "```";
#pragma warning restore SA1303

	public static string ExtractJson(string response)
	{
		var trimmed = response.Trim();

		if (trimmed.StartsWith(jsonFencePrefix, StringComparison.OrdinalIgnoreCase))
		{
			trimmed = trimmed[jsonFencePrefix.Length..];
		}
		else if (trimmed.StartsWith(fenceMarker, StringComparison.Ordinal))
		{
			trimmed = trimmed[fenceMarker.Length..];
		}

		if (trimmed.EndsWith(fenceMarker, StringComparison.Ordinal))
		{
			trimmed = trimmed[..^fenceMarker.Length];
		}

		return trimmed.Trim();
	}
}
