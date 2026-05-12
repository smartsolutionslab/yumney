namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public interface IHasDomainEvents
{
	IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

	void ClearDomainEvents();
}
