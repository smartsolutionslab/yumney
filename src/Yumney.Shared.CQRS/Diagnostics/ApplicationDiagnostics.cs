using System.Diagnostics;

namespace SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;

public static class ApplicationDiagnostics
{
    public const string SourceName = "Yumney.Application";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
