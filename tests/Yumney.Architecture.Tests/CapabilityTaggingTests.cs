using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

#pragma warning disable SA1601
public partial class CapabilityTaggingTests
#pragma warning restore SA1601
{
	[Fact]
	public void EveryWithCapabilityCall_HasUniqueName()
	{
		var srcRoot = SolutionRoot.Src;
		var pattern = WithCapabilityNamePattern();

		var allMatches = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.SelectMany(path => MatchesIn(path, pattern))
			.ToList();

		var duplicates = allMatches
			.GroupBy(match => match.Name, StringComparer.Ordinal)
			.Where(group => group.Count() > 1)
			.ToList();

		duplicates.Should().BeEmpty(
			"Capability names are LLM-facing tool identifiers and form an external contract — duplicates make the manifest ambiguous. Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, duplicates.Select(group => $"  {group.Key}: {string.Join(", ", group.Select(match => match.Location))}")));
	}

	[Fact]
	public void EveryWithCapabilityCall_UsesSnakeCaseName()
	{
		var srcRoot = SolutionRoot.Src;
		var pattern = WithCapabilityNamePattern();
		var snakeCase = SnakeCasePattern();

		var violations = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
			.Where(IsTrackedSourceFile)
			.SelectMany(path => MatchesIn(path, pattern))
			.Where(match => !snakeCase.IsMatch(match.Name))
			.ToList();

		violations.Should().BeEmpty(
			"Capability names must be snake_case (e.g. search_recipes, get_weekly_plan). Offenders:{0}{1}",
			Environment.NewLine,
			string.Join(Environment.NewLine, violations.Select(violation => $"  {violation.Name} at {violation.Location}")));
	}

	[GeneratedRegex(@"WithCapability\s*\(\s*name:\s*""(?<name>[^""]+)""", RegexOptions.Singleline)]
	private static partial Regex WithCapabilityNamePattern();

	[GeneratedRegex(@"^[a-z][a-z0-9_]*$")]
	private static partial Regex SnakeCasePattern();

	private static IEnumerable<CapabilityMatch> MatchesIn(string path, Regex pattern)
	{
		var content = File.ReadAllText(path);
		var matches = pattern.Matches(content);
		foreach (Match match in matches)
		{
			yield return new CapabilityMatch(match.Groups["name"].Value, Path.GetRelativePath(SolutionRoot.Path, path));
		}
	}

	private static bool IsTrackedSourceFile(string path)
	{
		var normalized = path.Replace('\\', '/');
		return !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
			&& !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
	}

	private sealed record CapabilityMatch(string Name, string Location);
}
