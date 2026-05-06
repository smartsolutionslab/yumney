using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Persisted event row for the ShoppingList aggregate stream. Mirrors the shape of
/// <see cref="SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.StoredEvent"/>
/// but maps to its own table so the ShoppingList stream stays separate from the
/// ShoppingLedger stream.
/// </summary>
public sealed class ShoppingListStoredEvent : IStoredEvent
{
	public Guid Id { get; set; }

	public Guid AggregateId { get; set; }

	public string EventType { get; set; } = default!;

	public string EventData { get; set; } = default!;

	public int Version { get; set; }

	public DateTime OccurredAt { get; set; }
}
