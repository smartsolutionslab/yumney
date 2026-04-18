using System.Diagnostics;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public static class ExtractionDiagnostics
{
	public const string SourceName = "Yumney.Recipes.Extraction";

	public static readonly ActivitySource ActivitySource = new(SourceName);
}
