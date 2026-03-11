namespace SmartSolutionsLab.Yumney.Shared.Events;

public interface IIntegrationEvent
{
    Guid EventIdentifier { get; }

    DateTime OccurredOn { get; }
}
