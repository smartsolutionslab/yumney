using System.Reflection;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class CqrsSeparationTests
{
	private static readonly string[] ApplicationModules = ["Recipes", "Shopping", "Users", "MealPlan"];

	[Fact]
	public void QueryHandlers_DoNotDependOnUnitOfWork()
	{
		var unitOfWorkBase = typeof(IUnitOfWork);
		var queryHandlerInterface = typeof(IQueryHandler<,>);

		var violations = new List<string>();

		foreach (var module in ApplicationModules)
		{
			var assembly = Assembly.Load($"Yumney.{module}.Application");

			var queryHandlers = assembly.GetTypes()
				.Where(type => type is { IsClass: true, IsAbstract: false })
				.Where(type => type.GetInterfaces().Any(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == queryHandlerInterface))
				.ToList();

			foreach (var handler in queryHandlers)
			{
				var ctorParams = handler
					.GetConstructors()
					.SelectMany(ctor => ctor.GetParameters())
					.Select(parameter => parameter.ParameterType);

				var uowDependency = ctorParams.FirstOrDefault(type => unitOfWorkBase.IsAssignableFrom(type));
				if (uowDependency is not null)
				{
					violations.Add($"{handler.FullName} depends on {uowDependency.Name}");
				}
			}
		}

		violations.Should().BeEmpty(
			"IQueryHandler implementations must not take an IUnitOfWork dependency. " +
			"Queries are read-only; writes belong in command handlers.");
	}
}
