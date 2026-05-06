using System.Text.Json;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

internal static class ShoppingLedgerEventSerializer
{
#pragma warning disable SA1311
	public static JsonSerializerOptions Options { get; } = BuildOptions();

	public static IEventSerializer Instance { get; } = new JsonEventSerializer(
		Options,
		EventTypeRegistry.BuildFromAssembly(
			typeof(ShoppingItemAdded).Assembly,
			type => type.Namespace == typeof(ShoppingItemAdded).Namespace));
#pragma warning restore SA1311

	private static JsonSerializerOptions BuildOptions()
	{
		var options = EventSerializerDefaults.Options();
		options.Converters.Add(new ItemNameJsonConverter());
		options.Converters.Add(new AmountJsonConverter());
		options.Converters.Add(new UnitJsonConverter());
		options.Converters.Add(new RemovalReasonJsonConverter());
		options.Converters.Add(new ItemSourceJsonConverter());
		return options;
	}
}
