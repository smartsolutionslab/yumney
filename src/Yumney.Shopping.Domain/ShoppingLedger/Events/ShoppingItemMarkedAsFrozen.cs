using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

/// <summary>
/// Raised when a user marks an at-home item as frozen (US-341 override).
/// Quantities are unchanged; downstream the projection flips the category
/// to <c>frozen</c> and resets the freshness clock.
/// </summary>
public sealed record ShoppingItemMarkedAsFrozen(
	ItemName ItemName,
	Unit? Unit) : DomainEvent;
