namespace SmartSolutionsLab.Yumney.Shared.Events;

public interface IEventBus
{
	Task PublishAsync<TEvent>(TEvent busEvent, CancellationToken cancellationToken = default)
		where TEvent : IBusEvent;
}
