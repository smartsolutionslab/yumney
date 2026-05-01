using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

#pragma warning disable SA1601 // Partial elements should be documented ([GeneratedRegex] requires partial methods).
public partial class NamingConventionTests
#pragma warning restore SA1601
{
	[Fact]
	public void NoSourceFile_HasSingleLetterLambdaParameter()
	{
		var srcRoot = LocateRepoFolder("src");
		var pattern = SingleLetterLambdaPattern();

		var violations = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.SelectMany(path => FindViolations(path, pattern))
			.ToList();

		violations.Should().BeEmpty(
			"src/ must not use single-letter lambda parameters per CLAUDE.md (Naming Conventions). "
			+ "Use a descriptive domain noun (recipe, slot, ingredient, …). Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, violations.Select(violation => $"  {violation}")));
	}

	[Fact]
	public void NoTestFile_HasSingleLetterLambdaParameter()
	{
		var testsRoot = LocateRepoFolder("tests");
		var pattern = SingleLetterLambdaPattern();

		var violations = Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.SelectMany(path => FindViolations(path, pattern))
			.ToList();

		violations.Should().BeEmpty(
			"tests/ must not use single-letter lambda parameters per CLAUDE.md (Naming Conventions). Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, violations.Select(violation => $"  {violation}")));
	}

	[Fact]
	public void NoTestFile_BindsSut()
	{
		var testsRoot = LocateRepoFolder("tests");
		var pattern = SutBindingPattern();

		var violations = Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.SelectMany(path => FindViolations(path, pattern))
			.ToList();

		violations.Should().BeEmpty(
			"tests/ must not use the `sut` identifier per CLAUDE.md (Test naming). "
			+ "Use the type name lowercased (`var handler = …`, `var validator = …`). Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, violations.Select(violation => $"  {violation}")));
	}

	// LINQ + foreach lambda parameters of length 1 (a..z), excluding the underscore discard.
	// Anchored to the call-site syntax so identifiers like `t` inside string literals or
	// type parameters don't trip the check.
	[GeneratedRegex(@"\.(?:Select|Where|OrderBy|OrderByDescending|ThenBy|ThenByDescending|First|FirstOrDefault|Single|SingleOrDefault|Any|All|Count|Sum|Min|Max|GroupBy|SelectMany|SkipWhile|TakeWhile|Aggregate|ToHashSet|ToDictionary|ToLookup|Find)\(\s*([a-z])\s*=>")]
	private static partial Regex SingleLetterLambdaPattern();

	[GeneratedRegex(@"(?:\bvar\s+sut\b|\bsut\b\s*\.|\bprivate\s+(?:readonly\s+)?[\w<>?,\s]+\s+sut\b)")]
	private static partial Regex SutBindingPattern();

	private static IEnumerable<string> FindViolations(string path, Regex pattern)
	{
		var lines = File.ReadAllLines(path);
		for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
		{
			var line = lines[lineIndex];
			if (IsCommentLine(line)) continue;
			if (!pattern.IsMatch(line)) continue;

			yield return $"{Path.GetRelativePath(LocateRepoRoot(), path)}:{lineIndex + 1}  {line.Trim()}";
		}
	}

	private static bool IsCommentLine(string line)
	{
		var trimmed = line.TrimStart();
		return trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("/*", StringComparison.Ordinal) || trimmed.StartsWith('*');
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

	private static string LocateRepoFolder(string folderName)
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null)
		{
			var candidate = Path.Combine(current.FullName, folderName);
			if (Directory.Exists(candidate)) return candidate;
			current = current.Parent;
		}

		throw new DirectoryNotFoundException($"Could not locate '{folderName}' relative to test assembly.");
	}

	private static string LocateRepoRoot()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null)
		{
			if (Directory.Exists(Path.Combine(current.FullName, "src")) && Directory.Exists(Path.Combine(current.FullName, "tests")))
			{
				return current.FullName;
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate repository root relative to test assembly.");
	}
}
