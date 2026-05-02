using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class ExternalDependencyIsolationTests
{
	[Theory]
	[InlineData("Yumney.Recipes.Domain")]
	[InlineData("Yumney.Recipes.Application")]
	[InlineData("Yumney.Recipes.Api")]
	public void Layer_ShouldNotDependOn_SemanticKernel(string assemblyName)
	{
		var assembly = Assembly.Load(assemblyName);

		var result = Types.InAssembly(assembly)
			.ShouldNot()
			.HaveDependencyOn("Microsoft.SemanticKernel")
			.GetResult();

		result.IsSuccessful.Should().BeTrue(
			$"{assemblyName} must route LLM calls through abstractions in Application; " +
			"Semantic Kernel lives in Yumney.Recipes.Extraction only.");
	}

	[Theory]
	[InlineData("Yumney.Recipes.Domain")]
	[InlineData("Yumney.Recipes.Application")]
	public void Layer_ShouldNotDependOn_HtmlScrapers(string assemblyName)
	{
		var assembly = Assembly.Load(assemblyName);

		var angleResult = Types.InAssembly(assembly)
			.ShouldNot()
			.HaveDependencyOn("AngleSharp")
			.GetResult();

		angleResult.IsSuccessful.Should().BeTrue(
			$"{assemblyName} must not reference AngleSharp directly.");
	}
}
