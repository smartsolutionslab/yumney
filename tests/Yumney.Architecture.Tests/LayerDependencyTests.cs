using NetArchTest.Rules;
using Xunit;

namespace Yumney.Architecture.Tests;

public class LayerDependencyTests
{
    private const string DomainNamespace = "Yumney.Modules.Recipes.Domain";
    private const string ApplicationNamespace = "Yumney.Modules.Recipes.Application";
    private const string InfrastructureNamespace = "Yumney.Modules.Recipes.Infrastructure";

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        var result = Types.InNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void RecipesModule_ShouldNotDependOn_ShoppingModule()
    {
        var result = Types.InNamespace("Yumney.Modules.Recipes")
            .ShouldNot()
            .HaveDependencyOn("Yumney.Modules.Shopping")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ShoppingModule_ShouldNotDependOn_RecipesModule()
    {
        var result = Types.InNamespace("Yumney.Modules.Shopping")
            .ShouldNot()
            .HaveDependencyOn("Yumney.Modules.Recipes")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
