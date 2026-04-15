using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

public sealed record ShoppingItemConsumed(
    ItemName ItemName,
    Amount Quantity,
    Unit? Unit,
    string Source) : DomainEvent;
