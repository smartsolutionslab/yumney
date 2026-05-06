using System.Text.Json;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
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
			new IngredientCategoryJsonConverter(),
		},
	};

	private static readonly Dictionary<string, Type> EventTypeMap = new()
	{
		[nameof(ShoppingListCreated)] = typeof(ShoppingListCreated),
		[nameof(ListItemAdded)] = typeof(ListItemAdded),
		[nameof(ListItemChecked)] = typeof(ListItemChecked),
		[nameof(ListItemUnchecked)] = typeof(ListItemUnchecked),
		[nameof(ListItemCategoryChanged)] = typeof(ListItemCategoryChanged),
		[nameof(AllItemsChecked)] = typeof(AllItemsChecked),
		[nameof(AllItemsUnchecked)] = typeof(AllItemsUnchecked),
		[nameof(RecipeReferenceCleared)] = typeof(RecipeReferenceCleared),
	};

	public static IEventSerializer Instance { get; } = new JsonEventSerializer(Options, EventTypeMap);

#pragma warning restore SA1311

	public static IDomainEvent? Deserialize(string eventType, string eventData)
		=> Instance.Deserialize(eventType, eventData);
}
