namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public static class LlmResponseParser
{
    private const string JsonFencePrefix = "```json";
    private const string FenceMarker = "```";

    public static string ExtractJson(string response)
    {
        var trimmed = response.Trim();

        if (trimmed.StartsWith(JsonFencePrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[JsonFencePrefix.Length..];
        }
        else if (trimmed.StartsWith(FenceMarker, StringComparison.Ordinal))
        {
            trimmed = trimmed[FenceMarker.Length..];
        }

        if (trimmed.EndsWith(FenceMarker, StringComparison.Ordinal))
        {
            trimmed = trimmed[..^FenceMarker.Length];
        }

        return trimmed.Trim();
    }
}
