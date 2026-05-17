using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests;

public class RecipesDiagnosticsTests
{
	[Fact]
	public void SourceName_IsTheCanonicalYumneyRecipesConstant()
	{
		RecipesDiagnostics.SourceName.Should().Be("Yumney.Recipes");
	}

	[Fact]
	public void ActivitySource_NameMatchesSourceName()
	{
		RecipesDiagnostics.ActivitySource.Name.Should().Be(RecipesDiagnostics.SourceName);
	}
}
