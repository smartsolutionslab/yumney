using System.Diagnostics;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure;

public static class UsersDiagnostics
{
    public const string SourceName = "Yumney.Users";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
