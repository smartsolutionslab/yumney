using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class LayerDependencyTests
{
	private const string SharedNamespace = "SmartSolutionsLab.Yumney.Shared";

	private static readonly string[] Modules = ["Recipes", "Shopping", "Users", "MealPlan"];

	public static TheoryData<string, string> CrossModulePairs()
	{
		var data = new TheoryData<string, string>();

		foreach (var source in Modules)
		{
			foreach (var target in Modules)
			{
				if (source != target) { data.Add(source, target); }
			}
		}

		return data;
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void Domain_ShouldNotDependOn_Application(string module)
	{
		var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

		var result = Types.InAssembly(domainAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{module}.Application")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on {module}.Application");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void Domain_ShouldNotDependOn_Infrastructure(string module)
	{
		var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

		var result = Types.InAssembly(domainAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{module}.Infrastructure")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on {module}.Infrastructure");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void Domain_ShouldNotDependOn_Api(string module)
	{
		var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

		var result = Types.InAssembly(domainAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{module}.Api")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on {module}.Api");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void Application_ShouldNotDependOn_Infrastructure(string module)
	{
		var applicationAssembly = GetAssembly($"Yumney.{module}.Application");

		var result = Types.InAssembly(applicationAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{module}.Infrastructure")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{module}.Application must not depend on {module}.Infrastructure");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void Application_ShouldNotDependOn_Api(string module)
	{
		var applicationAssembly = GetAssembly($"Yumney.{module}.Application");

		var result = Types.InAssembly(applicationAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{module}.Api")
			.GetResult();

		result.IsSuccessful.Should().BeTrue(
			$"{module}.Application must not depend on {module}.Api");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void ApiRequests_MayDependOn_Domain_ForVoConversion(string module)
	{
		var apiAssembly = GetAssembly($"Yumney.{module}.Api");

		var result = Types.InAssembly(apiAssembly)
			.That()
			.ResideInNamespace($"SmartSolutionsLab.Yumney.{module}.Api.Requests")
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{module}.Infrastructure")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{module}.Api.Requests must not depend on {module}.Infrastructure");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void ApiRequests_ShouldNotDependOn_Infrastructure(string module)
	{
		var apiAssembly = GetAssembly($"Yumney.{module}.Api");

		var result = Types.InAssembly(apiAssembly)
			.That()
			.ResideInNamespace($"SmartSolutionsLab.Yumney.{module}.Api.Requests")
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{module}.Infrastructure")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{module}.Api.Requests must not depend on {module}.Infrastructure");
	}

	[Theory]
	[MemberData(nameof(CrossModulePairs))]
	public void Modules_ShouldNotDependOn_OtherModuleDomains(string sourceModule, string targetModule)
	{
		var domainAssembly = GetAssembly($"Yumney.{sourceModule}.Domain");

		var result = Types.InAssembly(domainAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{targetModule}.Domain")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{sourceModule}.Domain must not depend on {targetModule}.Domain");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void Domain_ShouldNotDependOn_SharedCqrs(string module)
	{
		var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

		var result = Types.InAssembly(domainAssembly)
			.ShouldNot()
			.HaveDependencyOn("SmartSolutionsLab.Yumney.Shared.CQRS")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on Shared.CQRS");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void Domain_ShouldNotDependOn_SharedEvents(string module)
	{
		var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

		var result = Types.InAssembly(domainAssembly)
			.ShouldNot()
			.HaveDependencyOn("SmartSolutionsLab.Yumney.Shared.Events")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on Shared.Events");
	}

	[Theory]
	[MemberData(nameof(CrossModulePairs))]
	public void Modules_ShouldNotDependOn_OtherModuleInfrastructure(string sourceModule, string targetModule)
	{
		var applicationAssembly = GetAssembly($"Yumney.{sourceModule}.Application");

		var result = Types.InAssembly(applicationAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{targetModule}.Infrastructure")
			.GetResult();

		result.IsSuccessful.Should().BeTrue($"{sourceModule}.Application must not depend on {targetModule}.Infrastructure");
	}

	[Theory]
	[MemberData(nameof(CrossModulePairs))]
	public void ModuleApplication_ShouldNotDependOn_OtherModuleDomain(string sourceModule, string targetModule)
	{
		var applicationAssembly = GetAssembly($"Yumney.{sourceModule}.Application");

		var result = Types.InAssembly(applicationAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{targetModule}.Domain")
			.GetResult();

		result.IsSuccessful.Should().BeTrue(
			$"{sourceModule}.Application must not depend on {targetModule}.Domain — module identifier types must stay isolated");
	}

	[Theory]
	[MemberData(nameof(CrossModulePairs))]
	public void ModuleApplication_ShouldNotDependOn_OtherModuleApplication(string sourceModule, string targetModule)
	{
		var applicationAssembly = GetAssembly($"Yumney.{sourceModule}.Application");

		var result = Types.InAssembly(applicationAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{targetModule}.Application")
			.GetResult();

		result.IsSuccessful.Should().BeTrue(
			$"{sourceModule}.Application must not depend on {targetModule}.Application — cross-module communication goes through Shared.Events");
	}

	[Theory]
	[MemberData(nameof(CrossModulePairs))]
	public void ModuleApi_ShouldNotDependOn_OtherModuleDomain(string sourceModule, string targetModule)
	{
		var apiAssembly = GetAssembly($"Yumney.{sourceModule}.Api");

		var result = Types.InAssembly(apiAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{targetModule}.Domain")
			.GetResult();

		result.IsSuccessful.Should().BeTrue(
			$"{sourceModule}.Api must not depend on {targetModule}.Domain — module identifier types must stay isolated");
	}

	[Theory]
	[MemberData(nameof(CrossModulePairs))]
	public void ModuleApi_ShouldNotDependOn_OtherModuleApplication(string sourceModule, string targetModule)
	{
		var apiAssembly = GetAssembly($"Yumney.{sourceModule}.Api");

		var result = Types.InAssembly(apiAssembly)
			.ShouldNot()
			.HaveDependencyOn($"SmartSolutionsLab.Yumney.{targetModule}.Application")
			.GetResult();

		result.IsSuccessful.Should().BeTrue(
			$"{sourceModule}.Api must not depend on {targetModule}.Application");
	}

	private static System.Reflection.Assembly GetAssembly(string name)
	{
		return System.Reflection.Assembly.Load(name);
	}
}
