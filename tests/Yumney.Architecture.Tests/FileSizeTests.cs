using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class FileSizeTests
{
	private const int MaxLinesPerFile = 300;

	[Fact]
	public void NoSourceFile_Exceeds300Lines()
	{
		var oversized = Directory.EnumerateFiles(SolutionRoot.Src, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.Select(path => new { Path = path, LineCount = File.ReadAllLines(path).Length })
			.Where(file => file.LineCount > MaxLinesPerFile)
			.OrderByDescending(file => file.LineCount)
			.ToList();

		oversized.Should().BeEmpty(
			"files exceeding {0} lines per CLAUDE.md: {1}",
			MaxLinesPerFile,
			string.Join(Environment.NewLine, oversized.Select(file => $"  {file.LineCount} lines — {Path.GetRelativePath(SolutionRoot.Src, file.Path)}")));
	}

	private static bool IsTrackedSourceFile(string path)
	{
		var normalized = path.Replace('\\', '/');
		return !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
			&& !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
			&& !normalized.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
			&& !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
			&& !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
	}
}
