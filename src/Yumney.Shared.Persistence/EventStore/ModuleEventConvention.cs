using System.Linq.Expressions;
using System.Reflection;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

/// <summary>
/// Builds a domain-event-type → module-event-factory map by scanning an assembly
/// for IModuleEvent classes whose constructor matches the supplied context shape
/// followed by a single IDomainEvent-derived parameter.
/// </summary>
public static class ModuleEventConvention
{
	public delegate IModuleEvent ModuleEventFactory(IReadOnlyList<object> context, IDomainEvent domainEvent);

	public static IReadOnlyDictionary<Type, ModuleEventFactory> BuildMap(
		Assembly moduleEventsAssembly,
		params Type[] contextParameterTypes)
	{
		var map = new Dictionary<Type, ModuleEventFactory>();

		var moduleEventTypes = moduleEventsAssembly.GetTypes()
			.Where(type => typeof(IModuleEvent).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface);

		foreach (var moduleEventType in moduleEventTypes)
		{
			var ctor = moduleEventType
				.GetConstructors()
				.FirstOrDefault(constructor => MatchesShape(constructor, contextParameterTypes));
			if (ctor is null) continue;

			var domainType = ctor.GetParameters()[^1].ParameterType;
			map[domainType] = Compile(ctor, contextParameterTypes);
		}

		return map;
	}

	private static bool MatchesShape(ConstructorInfo ctor, Type[] contextParameterTypes)
	{
		var parameters = ctor.GetParameters();
		if (parameters.Length != contextParameterTypes.Length + 1) return false;

		for (var i = 0; i < contextParameterTypes.Length; i++)
		{
			if (parameters[i].ParameterType != contextParameterTypes[i]) return false;
		}

		return typeof(IDomainEvent).IsAssignableFrom(parameters[^1].ParameterType);
	}

	private static ModuleEventFactory Compile(ConstructorInfo ctor, Type[] contextParameterTypes)
	{
		var contextParam = Expression.Parameter(typeof(IReadOnlyList<object>), "ctx");
		var eventParam = Expression.Parameter(typeof(IDomainEvent), "ev");

		var indexer = typeof(IReadOnlyList<object>).GetProperty("Item")!;
		var args = new Expression[contextParameterTypes.Length + 1];
		for (var i = 0; i < contextParameterTypes.Length; i++)
		{
			var element = Expression.MakeIndex(contextParam, indexer, [Expression.Constant(i)]);
			args[i] = Expression.Convert(element, contextParameterTypes[i]);
		}

		var domainType = ctor.GetParameters()[^1].ParameterType;
		args[^1] = Expression.Convert(eventParam, domainType);

		var newExpr = Expression.New(ctor, args);
		var lambda = Expression.Lambda<ModuleEventFactory>(newExpr, contextParam, eventParam);
		return lambda.Compile();
	}
}
