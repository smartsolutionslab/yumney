namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public interface IDomainEvent
{
	DateTime OccurredOn { get; }
}
