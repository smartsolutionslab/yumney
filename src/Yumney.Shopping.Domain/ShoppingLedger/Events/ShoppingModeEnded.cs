using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

/// <summary>
/// Raised when user exits shopping mode.
/// </summary>
public sealed record ShoppingModeEnded(bool AcceptedPendingChanges) : DomainEvent;
