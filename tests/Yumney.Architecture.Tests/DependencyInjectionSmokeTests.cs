using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.MealPlan.Application;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure;
using SmartSolutionsLab.Yumney.Recipes.Application;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Application;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

/// <summary>
/// Locks in the auto-discovered DI registrations behind the Phase 2 helpers.
/// For each module, builds the same DI graph the real Program.cs assembles
/// (minus Aspire/Keycloak), then asserts that every
/// <c>IIntegrationEventHandler&lt;T&gt;</c> and <c>IModuleEventHandler&lt;T&gt;</c>
/// implementation discoverable in {Module}.Application + {Module}.Infrastructure
/// is resolvable as each of the interfaces it implements.
/// Catches the case where the assembly-scanner type-arg drifts away from the
/// module's own assembly (e.g. accidental copy-paste between modules).
/// </summary>
public class DependencyInjectionSmokeTests
{
	[Fact]
	public void RecipesModule_RegistersEveryDiscoveredHandler()
	{
		ServiceCollection services = [];
		services.AddRecipesApplication();
		services.AddRecipesInfrastructure(EmptyConfiguration());

		AssertEveryHandlerInterfaceIsRegistered(services, ["Yumney.Recipes.Application", "Yumney.Recipes.Infrastructure"]);
	}

	[Fact]
	public void ShoppingModule_RegistersEveryDiscoveredHandler()
	{
		ServiceCollection services = [];
		services.AddShoppingApplication();
		services.AddShoppingInfrastructure(EmptyConfiguration());

		AssertEveryHandlerInterfaceIsRegistered(services, ["Yumney.Shopping.Application", "Yumney.Shopping.Infrastructure"]);
	}

	[Fact]
	public void MealPlanModule_RegistersEveryDiscoveredHandler()
	{
		ServiceCollection services = [];
		services.AddMealPlanApplication();
		services.AddMealPlanInfrastructure(EmptyConfiguration());

		AssertEveryHandlerInterfaceIsRegistered(services, ["Yumney.MealPlan.Application", "Yumney.MealPlan.Infrastructure"]);
	}

	private static IConfiguration EmptyConfiguration() =>
		new ConfigurationBuilder().AddInMemoryCollection().Build();

	private static void AssertEveryHandlerInterfaceIsRegistered(IServiceCollection services, IReadOnlyList<string> assemblyNames)
	{
		Type[] openHandlers = [typeof(IIntegrationEventHandler<>), typeof(IModuleEventHandler<>)];
		List<(Type Implementation, Type Interface)> expected = [];
		foreach (var assemblyName in assemblyNames)
		{
			var assembly = Assembly.Load(assemblyName);
			expected.AddRange(assembly.GetTypes()
				.Where(type => type is { IsAbstract: false, IsInterface: false })
				.SelectMany(type => type.GetInterfaces()
					.Where(iface => iface.IsGenericType && openHandlers.Contains(iface.GetGenericTypeDefinition()))
					.Select(iface => (Implementation: type, Interface: iface))));
		}

		expected.Should().NotBeEmpty("at least one handler is expected so the test is meaningful");

		foreach (var (implementation, iface) in expected)
		{
			var because =
				$"{iface.FullName} (from {implementation.FullName}) must be registered in DI — " +
				"the auto-scanner most likely missed the assembly. Check that the relevant Add*() " +
				"call passes a type from the right assembly to AddIntegrationEventHandlersFromAssemblyContaining<T>.";
			services.Should().Contain(descriptor => descriptor.ServiceType == iface, because);
		}
	}
}
