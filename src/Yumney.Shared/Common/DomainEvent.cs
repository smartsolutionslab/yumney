using System;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public abstract record DomainEvent : IDomainEvent
{
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
