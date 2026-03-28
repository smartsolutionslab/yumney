using System.Diagnostics;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure;

public static class ShoppingDiagnostics
{
    public const string SourceName = "Yumney.Shopping";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
