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
	public static JsonSerializerOptions Options { get; } = BuildOptions();

	public static IEventSerializer Instance { get; } = new JsonEventSerializer(
		Options,
		EventTypeRegistry.BuildFromAssembly(
			typeof(ShoppingListCreated).Assembly,
			type => type.Namespace == typeof(ShoppingListCreated).Namespace));
#pragma warning restore SA1311

	public static IDomainEvent? Deserialize(string eventType, string eventData)
		=> Instance.Deserialize(eventType, eventData);

	private static JsonSerializerOptions BuildOptions()
	{
		var options = EventSerializerDefaults.Options();
		options.Converters.Add(new StringValueObjectJsonConverter<OwnerIdentifier>(OwnerIdentifier.From));
		options.Converters.Add(new StringValueObjectJsonConverter<ShoppingListTitle>(ShoppingListTitle.From));
		options.Converters.Add(new StringValueObjectJsonConverter<ItemName>(ItemName.From));
		options.Converters.Add(new ShoppingListIdentifierJsonConverter());
		options.Converters.Add(new ShoppingListItemIdentifierJsonConverter());
		options.Converters.Add(new RecipeReferenceJsonConverter());
		options.Converters.Add(new AmountJsonConverter());
		options.Converters.Add(new UnitJsonConverter());
		options.Converters.Add(new IngredientCategoryJsonConverter());
		return options;
	}
}
