using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

/// <summary>
/// Raised when an item is added directly as "at home" (already purchased, forgot to track).
/// </summary>
public sealed record ShoppingItemAddedAsAtHome(
    ItemName ItemName,
    Quantity Quantity) : DomainEvent;
