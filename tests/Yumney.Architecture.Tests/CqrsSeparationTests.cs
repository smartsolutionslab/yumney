using System.Reflection;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
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
				.Where(t => t is { IsClass: true, IsAbstract: false })
				.Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == queryHandlerInterface))
				.ToList();

			foreach (var handler in queryHandlers)
			{
				var ctorParams = handler
					.GetConstructors()
					.SelectMany(c => c.GetParameters())
					.Select(p => p.ParameterType);

				var uowDependency = ctorParams.FirstOrDefault(t => unitOfWorkBase.IsAssignableFrom(t));
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
