using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

public sealed record ShoppingListCreatedEvent(
    ShoppingListIdentifier ShoppingListIdentifier,
    ShoppingListTitle Title) : DomainEvent;
