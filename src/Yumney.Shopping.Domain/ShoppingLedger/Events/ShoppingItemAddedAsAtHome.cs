using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

/// <summary>
/// Raised when an item is added directly as "at home" (already purchased, forgot to track).
/// </summary>
public sealed record ShoppingItemAddedAsAtHome(
    string ItemName,
    decimal Quantity,
    string? Unit) : DomainEvent;
