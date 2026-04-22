using System;
using System.Collections.Generic;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public abstract class EventSourcedAggregate<TId>
	where TId : notnull
{
	private readonly List<IDomainEvent> uncommittedEvents = [];
	private readonly Dictionary<Type, Action<IDomainEvent>> handlers = [];

	public TId Identifier { get; protected set; } = default!;

	public AggregateVersion Version { get; protected set; } = AggregateVersion.Zero();

	public IReadOnlyCollection<IDomainEvent> UncommittedEvents => uncommittedEvents.AsReadOnly();

	public void MarkCommitted()
	{
		uncommittedEvents.Clear();
	}

	protected void On<TEvent>(Action<TEvent> handler)
		where TEvent : IDomainEvent
	{
		handlers[typeof(TEvent)] = e => handler((TEvent)e);
	}

	protected void RaiseEvent(IDomainEvent @event)
	{
		Apply(@event);
		uncommittedEvents.Add(@event);
		Version = Version.Increment();
	}

	// Replays events without buffering them as uncommitted — use from rehydration factories.
	protected void LoadFromHistory(IEnumerable<IDomainEvent> events, AggregateVersion? startVersion = null)
	{
		Version = startVersion ?? AggregateVersion.Zero();
		foreach (var @event in events)
		{
			Apply(@event);
			Version = Version.Increment();
		}
	}

	private void Apply(IDomainEvent @event)
	{
		if (handlers.TryGetValue(@event.GetType(), out var handler))
		{
			handler(@event);
		}
	}
}
