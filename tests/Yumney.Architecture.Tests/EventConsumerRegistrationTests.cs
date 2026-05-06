using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

public class EventConsumerRegistrationTests
{
	private static readonly string[] Modules = ["Recipes", "Shopping", "Users", "MealPlan"];

	[Fact]
	public void IIntegrationEvent_AndIModuleEvent_AreDisjointOnConcreteTypes()
	{
		// Concrete bus events are either cross-module (IIntegrationEvent) or
		// in-module (IModuleEvent), never both. Mixing the two markers on a single
		// type would cause InProcessEventBus to dispatch into both handler buckets,
		// silently fanning a single publish into two semantically different paths.
		var (eventAssemblies, _) = LoadAssemblies();

		var ambiguous = eventAssemblies
			.SelectMany(assembly => assembly.GetTypes())
			.Where(type => type is { IsClass: true, IsAbstract: false })
			.Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type) && typeof(IModuleEvent).IsAssignableFrom(type))
			.Select(type => type.FullName)
			.ToList();

		ambiguous.Should().BeEmpty(
			"a concrete event must implement IIntegrationEvent (cross-module contract) " +
			"or IModuleEvent (in-module bus envelope), never both — the two represent " +
			"different communication contracts and the bus dispatches them via separate handler interfaces.");
	}

	[Theory]
	[InlineData(typeof(IIntegrationEvent), typeof(IIntegrationEventHandler<>))]
	[InlineData(typeof(IModuleEvent), typeof(IModuleEventHandler<>))]
	public void EveryEvent_HasAtLeastOneHandler(Type eventBaseType, Type handlerInterfaceType)
	{
		var (eventAssemblies, handlerAssemblies) = LoadAssemblies();

		var events = eventAssemblies
			.SelectMany(assembly => assembly.GetTypes())
			.Where(type => eventBaseType.IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false })
			.ToList();

		var handledEventTypes = handlerAssemblies
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(iface => iface.GetGenericArguments()[0])
			.ToHashSet();

		events.Should().NotBeEmpty($"at least one {eventBaseType.Name} must exist across modules");

		var missing = events
			.Where(eventType => !handledEventTypes.Contains(eventType))
			.Select(eventType => eventType.FullName)
			.ToList();

		missing.Should().BeEmpty(
			$"every {eventBaseType.Name} must have at least one {handlerInterfaceType.Name} implementation. " +
			"Events without handlers will silently drop at runtime.");
	}

	[Theory]
	[InlineData(typeof(IIntegrationEvent), typeof(IIntegrationEventHandler<>))]
	[InlineData(typeof(IModuleEvent), typeof(IModuleEventHandler<>))]
	public void EveryEventHandler_TargetsAKnownEventType(Type eventBaseType, Type handlerInterfaceType)
	{
		var (_, handlerAssemblies) = LoadAssemblies();

		var handlerEventTargets = handlerAssemblies
			.SelectMany(assembly => assembly.GetTypes())
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterfaceType)
			.Select(iface => iface.GetGenericArguments()[0])
			.Distinct()
			.ToList();

		var nonEventHandlerTargets = handlerEventTargets
			.Where(type => !eventBaseType.IsAssignableFrom(type))
			.Select(type => type.FullName)
			.ToList();

		nonEventHandlerTargets.Should().BeEmpty(
			$"every {handlerInterfaceType.Name} must target a type implementing {eventBaseType.Name}");
	}

	[Theory]
	[InlineData("Recipes")]
	[InlineData("Shopping")]
	[InlineData("Users")]
	[InlineData("MealPlan")]
	public void AddYumneyDefaults_PassesEveryAssemblyWithHandlers(string module)
	{
		Type[] handlerInterfaceTypes = [typeof(IIntegrationEventHandler<>), typeof(IModuleEventHandler<>)];
		var moduleAssemblies = new[] { $"Yumney.{module}.Application", $"Yumney.{module}.Infrastructure" }
			.Select(name => Assembly.Load(name))
			.ToList();

		var requiredAssemblies = moduleAssemblies
			.Where(assembly => handlerInterfaceTypes.Any(iface => HasBusEventHandlers(assembly, iface)))
			.Select(assembly => assembly.GetName().Name!)
			.ToHashSet();

		var programText = File.ReadAllText(Path.Combine(SolutionRoot.Src, $"Yumney.{module}.Api", "Program.cs"));
		var argumentList = ExtractAddYumneyDefaultsArguments(programText);
		var coveredAssemblies = ResolveTypeofAssemblyNames(argumentList, moduleAssemblies);

		foreach (var required in requiredAssemblies)
		{
			var because = $"src/Yumney.{module}.Api/Program.cs must pass typeof(SomethingIn{required}).Assembly to AddYumneyDefaults — " +
				$"otherwise event handlers in {required} will never be wired into Wolverine.";
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

	private static bool HasBusEventHandlers(Assembly assembly, Type handlerInterfaceType) =>
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
		var matches = Regex.Matches(argumentList, @"typeof\s*\(\s*([A-Za-z_][\w.]*)\s*\)");
		foreach (Match match in matches)
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
