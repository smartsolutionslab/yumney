namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
	where TId : notnull
{
	private readonly List<IDomainEvent> domainEvents = [];

	public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

	public void ClearDomainEvents()
	{
		domainEvents.Clear();
	}

	protected static void CheckRule(IBusinessRule rule)
	{
		if (rule.IsBroken()) throw new BusinessRuleValidationException(rule);
	}

	protected void AddDomainEvent(IDomainEvent domainEvent)
	{
		domainEvents.Add(domainEvent);
	}
}
