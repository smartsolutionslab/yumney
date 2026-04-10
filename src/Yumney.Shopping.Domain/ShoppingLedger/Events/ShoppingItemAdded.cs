using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

public sealed record ShoppingItemAdded(
    string ItemName,
    decimal Quantity,
    string? Unit,
    string Source) : DomainEvent;
