namespace SmartSolutionsLab.Yumney.Shared.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
