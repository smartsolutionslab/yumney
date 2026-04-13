using System.Diagnostics;

namespace SmartSolutionsLab.Yumney.Shared.Events;

public static class EventsDiagnostics
{
    public const string SourceName = "Yumney.Events";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
