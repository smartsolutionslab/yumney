using System;

namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public abstract record DomainEvent : IDomainEvent
{
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
