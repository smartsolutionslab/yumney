using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Yumney.Architecture.Tests;

public class LayerDependencyTests
{
    private const string SharedNamespace = "Yumney.Shared";

    private static readonly string[] Modules = ["Recipes", "Shopping", "Users"];

    public static TheoryData<string, string> CrossModulePairs()
    {
        var data = new TheoryData<string, string>();

        foreach (var source in Modules)
        {
            foreach (var target in Modules)
            {
                if (source != target)
                {
                    data.Add(source, target);
                }
            }
        }

        return data;
    }

    [Theory]
    [InlineData("Recipes")]
    [InlineData("Shopping")]
    [InlineData("Users")]
    public void Domain_ShouldNotDependOn_Application(string module)
    {
        var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn($"Yumney.{module}.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on {module}.Application");
    }

    [Theory]
    [InlineData("Recipes")]
    [InlineData("Shopping")]
    [InlineData("Users")]
    public void Domain_ShouldNotDependOn_Infrastructure(string module)
    {
        var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn($"Yumney.{module}.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on {module}.Infrastructure");
    }

    [Theory]
    [InlineData("Recipes")]
    [InlineData("Shopping")]
    [InlineData("Users")]
    public void Domain_ShouldNotDependOn_Api(string module)
    {
        var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn($"Yumney.{module}.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on {module}.Api");
    }

    [Theory]
    [InlineData("Recipes")]
    [InlineData("Shopping")]
    [InlineData("Users")]
    public void Application_ShouldNotDependOn_Infrastructure(string module)
    {
        var applicationAssembly = GetAssembly($"Yumney.{module}.Application");

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn($"Yumney.{module}.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"{module}.Application must not depend on {module}.Infrastructure");
    }

    [Theory]
    [InlineData("Recipes")]
    [InlineData("Shopping")]
    [InlineData("Users")]
    public void Application_ShouldNotDependOn_Api(string module)
    {
        var applicationAssembly = GetAssembly($"Yumney.{module}.Application");

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn($"Yumney.{module}.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Application must not depend on {module}.Api");
    }

    [Theory]
    [MemberData(nameof(CrossModulePairs))]
    public void Modules_ShouldNotDependOn_OtherModuleDomains(string sourceModule, string targetModule)
    {
        var domainAssembly = GetAssembly($"Yumney.{sourceModule}.Domain");

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn($"Yumney.{targetModule}.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"{sourceModule}.Domain must not depend on {targetModule}.Domain");
    }

    [Theory]
    [InlineData("Recipes")]
    [InlineData("Shopping")]
    [InlineData("Users")]
    public void Domain_ShouldNotDependOn_SharedCqrs(string module)
    {
        var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Yumney.Shared.CQRS")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"{module}.Domain must not depend on Shared.CQRS");
    }

    [Theory]
    [InlineData("Recipes")]
    [InlineData("Shopping")]
    [InlineData("Users")]
    public void Domain_ShouldNotDependOn_SharedEvents(string module)
    {
        var domainAssembly = GetAssembly($"Yumney.{module}.Domain");

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Yumney.Shared.Events")
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
            .HaveDependencyOn($"Yumney.{targetModule}.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"{sourceModule}.Application must not depend on {targetModule}.Infrastructure");
    }

    private static System.Reflection.Assembly GetAssembly(string name)
    {
        return System.Reflection.Assembly.Load(name);
    }
}
