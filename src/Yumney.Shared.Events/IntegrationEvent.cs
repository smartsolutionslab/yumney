namespace SmartSolutionsLab.Yumney.Shared.Events;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventIdentifier { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
