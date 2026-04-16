using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

/// <summary>
/// Raised when a bought item is reverted to unbought.
/// </summary>
public sealed record ShoppingItemUndoBought(
    ItemName ItemName,
    Quantity Quantity) : DomainEvent;
