using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class FileSizeTests
{
    private const int MaxLinesPerFile = 300;

    /// <summary>
    /// CLAUDE.md mandates a 300-line maximum for source files. This test scans
    /// every tracked .cs file under src/ and fails with the offending list.
    /// </summary>
    [Fact]
    public void NoSourceFile_Exceeds300Lines()
    {
        var srcRoot = LocateSourceRoot();

        var oversized = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(IsTrackedSourceFile)
            .Select(path => new { Path = path, LineCount = File.ReadAllLines(path).Length })
            .Where(file => file.LineCount > MaxLinesPerFile)
            .OrderByDescending(file => file.LineCount)
            .ToList();

        oversized.Should().BeEmpty(
            "files exceeding {0} lines per CLAUDE.md: {1}",
            MaxLinesPerFile,
            string.Join(Environment.NewLine, oversized.Select(f => $"  {f.LineCount} lines — {Path.GetRelativePath(srcRoot, f.Path)}")));
    }

    private static bool IsTrackedSourceFile(string path)
    {
        // Skip generated, build output, migration scaffolding, and designer files.
        var normalized = path.Replace('\\', '/');
        return !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
            && !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
            && !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string LocateSourceRoot()
    {
        // Walk upward from the test assembly location until we find the repo's `src` folder.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src");
            if (Directory.Exists(candidate)) return candidate;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate 'src' folder relative to test assembly.");
    }
}
