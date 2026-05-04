using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed class ShoppingList : EventSourcedAggregate<ShoppingListIdentifier>
{
	private readonly List<ShoppingListItem> items = [];

	public ShoppingListTitle Title { get; private set; } = default!;

	public OwnerIdentifier Owner { get; private set; } = default!;

	public RecipeReference? RecipeReference { get; private set; }

	public DateTime CreatedAt { get; private set; }

	public IReadOnlyList<ShoppingListItem> Items => items.AsReadOnly();

	private ShoppingList()
	{
		On<ShoppingListCreated>(OnCreated);
		On<ListItemAdded>(OnListItemAdded);
		On<ListItemChecked>(OnListItemChecked);
		On<ListItemUnchecked>(OnListItemUnchecked);
		On<ListItemCategoryChanged>(OnListItemCategoryChanged);
		On<AllItemsChecked>(_ => OnAllItemsChecked());
		On<AllItemsUnchecked>(_ => OnAllItemsUnchecked());
		On<RecipeReferenceCleared>(_ => OnRecipeReferenceCleared());
	}

	public static ShoppingList Create(
		ShoppingListTitle title,
		OwnerIdentifier owner,
		IReadOnlyList<ShoppingListItem> items,
		RecipeReference? recipeReference = null)
	{
		Ensure.That(items).IsNotEmpty();

		var list = new ShoppingList();
		list.RaiseEvent(new ShoppingListCreated(
			ShoppingListIdentifier.New(),
			title,
			owner,
			recipeReference,
			DateTime.UtcNow));

		foreach (var item in items)
		{
			list.RaiseEvent(new ListItemAdded(item.Id, item.Name, item.Quantity, item.Category));
		}

		return list;
	}

	public static ShoppingList FromEvents(
		ShoppingListIdentifier identifier,
		IEnumerable<IDomainEvent> events,
		AggregateVersion? startVersion = null)
	{
		var list = new ShoppingList { Identifier = identifier };
		list.LoadFromHistory(events, startVersion);
		return list;
	}

	public ShoppingList CheckOffItem(ShoppingListItemIdentifier itemId)
	{
		EnsureItemExists(itemId);
		RaiseEvent(new ListItemChecked(itemId));
		return this;
	}

	public ShoppingList UncheckItem(ShoppingListItemIdentifier itemId)
	{
		EnsureItemExists(itemId);
		RaiseEvent(new ListItemUnchecked(itemId));
		return this;
	}

	public ShoppingList CheckAllItems()
	{
		RaiseEvent(new AllItemsChecked());
		return this;
	}

	public ShoppingList UncheckAllItems()
	{
		RaiseEvent(new AllItemsUnchecked());
		return this;
	}

	public ShoppingList ChangeItemCategory(ShoppingListItemIdentifier itemId, IngredientCategory category)
	{
		EnsureItemExists(itemId);
		RaiseEvent(new ListItemCategoryChanged(itemId, category));
		return this;
	}

	public ShoppingList ClearRecipeReference()
	{
		RaiseEvent(new RecipeReferenceCleared());
		return this;
	}

	private void OnCreated(ShoppingListCreated e)
	{
		Identifier = e.Identifier;
		Title = e.Title;
		Owner = e.Owner;
		RecipeReference = e.RecipeReference;
		CreatedAt = e.CreatedAt;
	}

	private void OnListItemAdded(ListItemAdded e)
	{
		items.Add(ShoppingListItem.Hydrate(e.ItemId, e.Name, e.Quantity, e.Category));
	}

	private void OnListItemCategoryChanged(ListItemCategoryChanged e)
	{
		FindItem(e.ItemId).ChangeCategoryTo(e.Category);
	}

	private void OnListItemChecked(ListItemChecked e)
	{
		FindItem(e.ItemId).MarkChecked();
	}

	private void OnListItemUnchecked(ListItemUnchecked e)
	{
		FindItem(e.ItemId).MarkUnchecked();
	}

	private void OnAllItemsChecked()
	{
		foreach (var item in items)
		{
			item.MarkChecked();
		}
	}

	private void OnAllItemsUnchecked()
	{
		foreach (var item in items)
		{
			item.MarkUnchecked();
		}
	}

	private void OnRecipeReferenceCleared()
	{
		RecipeReference = null;
	}

	private void EnsureItemExists(ShoppingListItemIdentifier itemId)
	{
		var item = items.FirstOrDefault(candidate => candidate.Id == itemId);
		Ensure.That(item!).IsNotNull();
	}

	private ShoppingListItem FindItem(ShoppingListItemIdentifier itemId)
	{
		var item = items.FirstOrDefault(candidate => candidate.Id == itemId);
		Ensure.That(item!).IsNotNull();
		return item!;
	}
}
