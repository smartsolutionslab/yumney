using System.Text.Json;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.Json;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Centralised JSON options + type map for ShoppingList event payloads. Shared
/// between the event store (write/read) and the projection rebuilder so both
/// agree on serialisation contract.
/// </summary>
internal static class ShoppingListEventSerializer
{
#pragma warning disable SA1311
	public static JsonSerializerOptions Options { get; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new StringValueObjectJsonConverter<OwnerIdentifier>(OwnerIdentifier.From),
			new StringValueObjectJsonConverter<ShoppingListTitle>(ShoppingListTitle.From),
			new StringValueObjectJsonConverter<ItemName>(ItemName.From),
			new ShoppingListIdentifierJsonConverter(),
			new ShoppingListItemIdentifierJsonConverter(),
			new RecipeReferenceJsonConverter(),
			new AmountJsonConverter(),
			new UnitJsonConverter(),
		},
	};

	private static readonly Dictionary<string, Type> eventTypeMap = new()
	{
		[nameof(ShoppingListCreated)] = typeof(ShoppingListCreated),
		[nameof(ListItemAdded)] = typeof(ListItemAdded),
		[nameof(ListItemChecked)] = typeof(ListItemChecked),
		[nameof(ListItemUnchecked)] = typeof(ListItemUnchecked),
		[nameof(AllItemsChecked)] = typeof(AllItemsChecked),
		[nameof(AllItemsUnchecked)] = typeof(AllItemsUnchecked),
		[nameof(RecipeReferenceCleared)] = typeof(RecipeReferenceCleared),
	};

#pragma warning restore SA1311

	public static string Serialize(IDomainEvent @event) =>
		JsonSerializer.Serialize(@event, @event.GetType(), Options);

	public static IDomainEvent? Deserialize(string eventType, string eventData)
	{
		if (!eventTypeMap.TryGetValue(eventType, out var type)) return null;
		return JsonSerializer.Deserialize(eventData, type, Options) as IDomainEvent;
	}
}
