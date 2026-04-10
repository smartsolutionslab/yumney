using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

public sealed record ShoppingItemRemoved(
    string ItemName,
    decimal Quantity,
    string? Unit,
    string? Reason) : DomainEvent;
