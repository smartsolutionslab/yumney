using System.Text.Json;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

internal static class ShoppingLedgerEventSerializer
{
#pragma warning disable SA1311
	public static JsonSerializerOptions Options { get; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new ItemNameJsonConverter(),
			new AmountJsonConverter(),
			new UnitJsonConverter(),
			new RemovalReasonJsonConverter(),
			new ItemSourceJsonConverter(),
		},
	};

	private static readonly Dictionary<string, Type> EventTypeMap = new()
	{
		[nameof(ShoppingItemAdded)] = typeof(ShoppingItemAdded),
		[nameof(ShoppingItemBought)] = typeof(ShoppingItemBought),
		[nameof(ShoppingItemConsumed)] = typeof(ShoppingItemConsumed),
		[nameof(ShoppingItemRemoved)] = typeof(ShoppingItemRemoved),
		[nameof(ShoppingItemQuantityAdjusted)] = typeof(ShoppingItemQuantityAdjusted),
		[nameof(ShoppingItemUndoBought)] = typeof(ShoppingItemUndoBought),
		[nameof(ShoppingItemAddedAsAtHome)] = typeof(ShoppingItemAddedAsAtHome),
		[nameof(ShoppingItemMarkedAsFrozen)] = typeof(ShoppingItemMarkedAsFrozen),
		[nameof(ShoppingModeStarted)] = typeof(ShoppingModeStarted),
		[nameof(ShoppingModeEnded)] = typeof(ShoppingModeEnded),
	};

	public static IEventSerializer Instance { get; } = new JsonEventSerializer(Options, EventTypeMap);
#pragma warning restore SA1311
}
