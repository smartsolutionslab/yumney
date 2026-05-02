using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class EventConsumerRegistrationTests
{
	private static readonly string[] Modules = ["Recipes", "Shopping", "Users", "MealPlan"];

	[Fact]
	public void EveryIntegrationEvent_HasAtLeastOneHandler()
	{
		var (eventAssemblies, handlerAssemblies) = LoadAssemblies();

		var integrationEventType = typeof(IIntegrationEvent);
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

		var integrationEvents = eventAssemblies
			.SelectMany(assembly => assembly.GetTypes())
			.Where(type => integrationEventType.IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false })
			.ToList();

		var handledEventTypes = handlerAssemblies
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(iface => iface.GetGenericArguments()[0])
			.ToHashSet();

		integrationEvents.Should().NotBeEmpty("at least one integration event must exist across modules");

		var missing = integrationEvents
			.Where(eventType => !handledEventTypes.Contains(eventType))
			.Select(eventType => eventType.FullName)
			.ToList();

		missing.Should().BeEmpty(
			"every IIntegrationEvent must have at least one IIntegrationEventHandler<T> implementation. " +
			"Events without handlers will silently drop at runtime.");
	}

	[Fact]
	public void EveryIntegrationEventHandler_HandlesAKnownIntegrationEvent()
	{
		var (_, handlerAssemblies) = LoadAssemblies();

		var integrationEventType = typeof(IIntegrationEvent);
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

		var handlerEventTargets = handlerAssemblies
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(iface => iface.GetGenericArguments()[0])
			.Distinct()
			.ToList();

		var nonEventHandlerTargets = handlerEventTargets
			.Where(type => !integrationEventType.IsAssignableFrom(type))
			.Select(type => type.FullName)
			.ToList();

		nonEventHandlerTargets.Should().BeEmpty(
			"every IIntegrationEventHandler<T> must target a type implementing IIntegrationEvent");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void AddYumneyDefaults_PassesEveryAssemblyWithHandlers(string module)
	{
		var handlerInterfaceType = typeof(IIntegrationEventHandler<>);
		var moduleAssemblies = new[] { $"Yumney.{module}.Application", $"Yumney.{module}.Infrastructure" }
			.Select(name => Assembly.Load(name))
			.ToList();

		var requiredAssemblies = moduleAssemblies
			.Where(assembly => HasIntegrationHandlers(assembly, handlerInterfaceType))
			.Select(assembly => assembly.GetName().Name!)
			.ToHashSet();

		var programText = File.ReadAllText(Path.Combine(SolutionRoot.Path, "src", $"Yumney.{module}.Api", "Program.cs"));
		var argumentList = ExtractAddYumneyDefaultsArguments(programText);
		var coveredAssemblies = ResolveTypeofAssemblyNames(argumentList, moduleAssemblies);

		foreach (var required in requiredAssemblies)
		{
			var because = $"src/Yumney.{module}.Api/Program.cs must pass typeof(SomethingIn{required}).Assembly to AddYumneyDefaults — " +
				$"otherwise integration event handlers in {required} will never be wired into Wolverine.";
			coveredAssemblies.Should().Contain(required, because);
		}
	}

	private static (List<Assembly> EventAssemblies, List<Assembly> HandlerAssemblies) LoadAssemblies()
	{
		List<Assembly> eventAssemblies = [Assembly.Load("Yumney.Shared.Events")];
		eventAssemblies.AddRange(Modules.Select(module => Assembly.Load($"Yumney.{module}.Infrastructure")));

		List<Assembly> handlerAssemblies = [];
		foreach (var module in Modules)
		{
			handlerAssemblies.Add(Assembly.Load($"Yumney.{module}.Application"));
			handlerAssemblies.Add(Assembly.Load($"Yumney.{module}.Infrastructure"));
		}

		return (eventAssemblies, handlerAssemblies);
	}

	private static bool HasIntegrationHandlers(Assembly assembly, Type handlerInterfaceType) =>
		assembly.GetTypes()
			.SelectMany(type => type.GetInterfaces())
			.Any(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType);

	private static string ExtractAddYumneyDefaultsArguments(string programText)
	{
		const string startMarker = "AddYumneyDefaults(";
		var startIndex = programText.IndexOf(startMarker, StringComparison.Ordinal);
		startIndex.Should().BeGreaterThanOrEqualTo(0, "Program.cs must call AddYumneyDefaults(...)");

		var contentStart = startIndex + startMarker.Length;
		var depth = 1;
		var index = contentStart;
		while (index < programText.Length && depth > 0)
		{
			var ch = programText[index];
			if (ch == '(') depth++;
			else if (ch == ')') depth--;
			if (depth == 0) break;
			index++;
		}

		return programText[contentStart..index];
	}

	private static HashSet<string> ResolveTypeofAssemblyNames(string argumentList, IEnumerable<Assembly> candidateAssemblies)
	{
		HashSet<string> covered = [];
		foreach (Match match in Regex.Matches(argumentList, @"typeof\s*\(\s*([A-Za-z_][\w.]*)\s*\)"))
		{
			var typeName = match.Groups[1].Value;
			var assembly = candidateAssemblies
				.FirstOrDefault(asm => asm.GetTypes().Any(type => type.Name == typeName || type.FullName == typeName));
			if (assembly is not null)
			{
				covered.Add(assembly.GetName().Name!);
			}
		}

		return covered;
	}
}

internal static class SolutionRoot
{
	public static string Path { get; } = Locate();

	private static string Locate([CallerFilePath] string callerFilePath = "")
	{
		var directory = new DirectoryInfo(System.IO.Path.GetDirectoryName(callerFilePath)!);
		while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "Yumney.slnx")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName
			?? throw new InvalidOperationException("Yumney.slnx not found walking up from test source location.");
	}
}
