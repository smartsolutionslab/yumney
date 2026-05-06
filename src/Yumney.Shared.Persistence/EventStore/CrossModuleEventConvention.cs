using System.Reflection;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public static class CrossModuleEventConvention
{
	public delegate IIntegrationEvent? CrossModuleEventFactory(IReadOnlyList<object> context, IDomainEvent domainEvent);

	public static IReadOnlyDictionary<Type, CrossModuleEventFactory> BuildMap(Assembly assembly)
	{
		var map = new Dictionary<Type, CrossModuleEventFactory>();

		var mapperTypes = assembly.GetTypes()
			.Where(type => typeof(ICrossModuleEventMapper).IsAssignableFrom(type)
				&& !type.IsAbstract
				&& !type.IsInterface
				&& type.GetConstructor(Type.EmptyTypes) is not null);

		foreach (var mapperType in mapperTypes)
		{
			var mapper = (ICrossModuleEventMapper)Activator.CreateInstance(mapperType)!;
			map[mapper.DomainEventType] = mapper.TryMap;
		}

		return map;
	}
}
