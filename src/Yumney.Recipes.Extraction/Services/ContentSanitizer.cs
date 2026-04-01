using System.Text.RegularExpressions;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
public static partial class ContentSanitizer
#pragma warning restore SA1601
{
    public static string Sanitize(string text)
    {
        var sanitized = InjectionPatterns().Replace(text, string.Empty);
        sanitized = ExcessiveWhitespace().Replace(sanitized, " ");
        return sanitized.Trim();
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex ExcessiveWhitespace();

    [GeneratedRegex(@"ignore previous instructions|ignore all instructions|disregard previous|system:|assistant:|<\|im_start\|>|<\|im_end\|>|<\|endoftext\|>", RegexOptions.IgnoreCase)]
    private static partial Regex InjectionPatterns();
}
