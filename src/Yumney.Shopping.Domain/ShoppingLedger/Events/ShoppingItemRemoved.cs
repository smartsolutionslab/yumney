using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

public sealed record ShoppingItemRemoved(
    ItemName ItemName,
    Quantity Quantity,
    RemovalReason? Reason) : DomainEvent;
