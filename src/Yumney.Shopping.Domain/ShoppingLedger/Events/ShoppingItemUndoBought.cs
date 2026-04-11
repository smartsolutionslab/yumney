using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

/// <summary>
/// Raised when a bought item is reverted to unbought.
/// </summary>
public sealed record ShoppingItemUndoBought(
    string ItemName,
    decimal Quantity,
    string? Unit) : DomainEvent;
