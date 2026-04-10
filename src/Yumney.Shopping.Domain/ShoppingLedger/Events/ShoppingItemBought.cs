using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

public sealed record ShoppingItemBought(
    string ItemName,
    decimal Quantity,
    string? Unit) : DomainEvent;
