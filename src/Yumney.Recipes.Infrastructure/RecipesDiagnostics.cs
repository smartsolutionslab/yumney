using System.Diagnostics;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure;

public static class RecipesDiagnostics
{
    public const string SourceName = "Yumney.Recipes";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
