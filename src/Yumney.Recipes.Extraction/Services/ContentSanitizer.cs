namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

/// <summary>
/// Prepares scraped page text for inclusion in an LLM prompt.
/// The primary defence against prompt injection is
/// <see cref="ExtractionPrompts.WrapInContentDelimiters"/> plus the
/// system-prompt instruction to ignore non-recipe content. This
/// sanitizer's job is to prevent the content from closing the
/// delimiter tags early — everything else (including line breaks,
/// which are meaningful structure for ingredient lists) stays.
/// </summary>
public static class ContentSanitizer
{
	/// <summary>
	/// Escapes the <c>&lt;webpage_content&gt;</c> / <c>&lt;/webpage_content&gt;</c>
	/// delimiters if they appear in the scraped text so a hostile page
	/// cannot inject a fake closing tag followed by new instructions.
	/// Collapses runs of more than two consecutive blank lines, which
	/// are almost certainly layout artefacts rather than content.
	/// </summary>
	/// <param name="text">The cleaned page text.</param>
	/// <returns>Sanitized text ready for wrapping in delimiters.</returns>
	public static string Sanitize(string text)
	{
		if (string.IsNullOrEmpty(text)) return string.Empty;

		var escaped = text
			.Replace("<webpage_content>", "<webpage_content_ESCAPED>", StringComparison.OrdinalIgnoreCase)
			.Replace("</webpage_content>", "</webpage_content_ESCAPED>", StringComparison.OrdinalIgnoreCase);

		return CollapseExcessiveBlankLines(escaped).Trim();
	}

	private static string CollapseExcessiveBlankLines(string text)
	{
		var sb = new System.Text.StringBuilder(text.Length);
		var blankRun = 0;

		foreach (var line in text.Split('\n'))
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				blankRun++;
				if (blankRun <= 2) sb.Append('\n');
				continue;
			}

			blankRun = 0;
			sb.Append(line.TrimEnd());
			sb.Append('\n');
		}

		return sb.ToString();
	}
}
