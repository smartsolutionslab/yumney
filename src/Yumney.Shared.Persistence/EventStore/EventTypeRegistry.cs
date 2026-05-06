using System.Reflection;
using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public static class EventTypeRegistry
{
	public static IReadOnlyDictionary<string, Type> BuildFromAssembly(Assembly assembly, Func<Type, bool>? filter = null)
		=> assembly.GetTypes()
			.Where(type => typeof(IDomainEvent).IsAssignableFrom(type)
				&& !type.IsAbstract
				&& !type.IsInterface
				&& (filter is null || filter(type)))
			.ToDictionary(type => type.Name);
}
