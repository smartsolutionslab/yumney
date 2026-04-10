using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

public sealed record ShoppingItemQuantityAdjusted(
    string ItemName,
    decimal NewQuantity,
    string? Unit) : DomainEvent;
