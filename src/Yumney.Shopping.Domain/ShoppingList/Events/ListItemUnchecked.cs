using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

public sealed record ListItemUnchecked(ShoppingListItemIdentifier ItemId) : DomainEvent;
