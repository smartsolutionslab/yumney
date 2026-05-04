using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

/// <summary>
/// Raised when user enters shopping mode — list becomes a snapshot.
/// </summary>
public sealed record ShoppingModeStarted(DateTime SnapshotTakenAt) : DomainEvent;
