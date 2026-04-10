using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

public sealed record ShoppingItemConsumed(
    string ItemName,
    decimal Quantity,
    string? Unit,
    string Source) : DomainEvent;
