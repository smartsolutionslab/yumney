using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

#pragma warning disable SA1601
public partial class ClaudeMdRuleEnforcementTests
#pragma warning restore SA1601
{
	[Fact]
	public void NoSourceFileInInfrastructureOrApi_CallsEnsureThat()
	{
		var srcRoot = LocateRepoFolder("src");
		var pattern = EnsureThatPattern();

		var violations = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.Where(IsInfrastructureOrApiFile)
			.SelectMany(path => FindViolations(path, pattern))
			.ToList();

		violations.Should().BeEmpty(
			"Ensure.That() is reserved for Domain and Application layers per CLAUDE.md (Guard System). "
			+ "Validate at boundaries via FluentValidation in Application or via VO constructors in Domain. Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, violations.Select(violation => $"  {violation}")));
	}

	[Fact]
	public void NoSourceFile_DeclaresRepositoryVariableWithRedundantSuffix()
	{
		var srcRoot = LocateRepoFolder("src");
		var pattern = RedundantRepositoryNamePattern();

		var violations = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.SelectMany(path => FindViolations(path, pattern))
			.ToList();

		violations.Should().BeEmpty(
			"Repository variables, fields, and parameters must be named after the plural aggregate (recipes, shoppingLists, …) per CLAUDE.md (Repository Naming). "
			+ "Names ending in `Repository` or `Repo`, or named exactly `repository`, are forbidden. Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, violations.Select(violation => $"  {violation}")));
	}

	[Fact]
	public void NoSourceFile_ProjectsInlineDtoInsideLinqSelect()
	{
		var srcRoot = LocateRepoFolder("src");
		var pattern = InlineDtoProjectionPattern();

		var violations = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.Where(IsNotMappingExtension)
			.SelectMany(path => FindViolations(path, pattern))
			.ToList();

		violations.Should().BeEmpty(
			"Inline `.Select(x => new XDto(...))` projections are forbidden outside `*MappingExtensions.cs` per CLAUDE.md (DTO Mapping). "
			+ "Move the projection into a `ToDto()` extension method on the source type. Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, violations.Select(violation => $"  {violation}")));
	}

	[Fact]
	public void NoDomainAggregateOrEntity_HasPublicOrInternalVoidMethod()
	{
		var srcRoot = LocateRepoFolder("src");
		var pattern = PublicOrInternalVoidMethodPattern();

		var violations = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.Where(IsDomainAggregateOrEntityFile)
			.SelectMany(path => FindViolations(path, pattern))
			.ToList();

		violations.Should().BeEmpty(
			"Domain aggregate and entity methods must return a value (this for fluent chaining, or a domain value) per CLAUDE.md (Domain Methods – Always Return a Value). "
			+ "Exceptions: base-class infrastructure methods (ClearDomainEvents, RaiseEvent) live in Shared, not in module Domain; "
			+ "private event handlers may remain void. Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, violations.Select(violation => $"  {violation}")));
	}

	[GeneratedRegex(@"\bEnsure\.That\(")]
	private static partial Regex EnsureThatPattern();

	[GeneratedRegex(@"\bI[A-Z]\w+Repository\s+(?:[a-z]\w*(?:Repository|Repo)\b|repository\b)")]
	private static partial Regex RedundantRepositoryNamePattern();

	[GeneratedRegex(@"\.Select\([^)=]*=>\s*new\s+\w+Dto\b")]
	private static partial Regex InlineDtoProjectionPattern();

	[GeneratedRegex(@"^\s*(?:public|internal)\s+(?!static\s)(?!abstract\s)(?!override\s)(?:async\s+)?void\s+\w+\s*\(")]
	private static partial Regex PublicOrInternalVoidMethodPattern();

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
		return trimmed.StartsWith("//", StringComparison.Ordinal)
			|| trimmed.StartsWith("/*", StringComparison.Ordinal)
			|| trimmed.StartsWith('*');
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

	private static bool IsInfrastructureOrApiFile(string path)
	{
		var normalized = path.Replace('\\', '/');
		return normalized.Contains(".Infrastructure/", StringComparison.OrdinalIgnoreCase)
			|| normalized.Contains(".Api/", StringComparison.OrdinalIgnoreCase)
			|| normalized.Contains(".Gateway/", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsNotMappingExtension(string path)
	{
		var normalized = path.Replace('\\', '/');
		return !path.EndsWith("MappingExtensions.cs", StringComparison.OrdinalIgnoreCase)
			&& !normalized.Contains("/ReadModel/", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsDomainAggregateOrEntityFile(string path)
	{
		var normalized = path.Replace('\\', '/');
		if (!normalized.Contains(".Domain/", StringComparison.OrdinalIgnoreCase)) return false;

		// Skip Events/, Rules/, Handlers/ subdirectories — event handlers and rule classes
		// have their own conventions and aren't aggregate methods.
		return !normalized.Contains("/Events/", StringComparison.OrdinalIgnoreCase)
			&& !normalized.Contains("/Rules/", StringComparison.OrdinalIgnoreCase)
			&& !normalized.Contains("/Handlers/", StringComparison.OrdinalIgnoreCase);
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
