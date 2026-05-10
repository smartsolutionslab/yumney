namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Marker for an in-module bus envelope. These wrap a Domain event and add
/// routing metadata (at minimum the owner) so projection handlers in the same
/// module can subscribe via the bus. They MUST NOT cross module boundaries —
/// cross-module contracts use <see cref="IIntegrationEvent"/> and live in
/// <c>Yumney.Shared.Events.Contracts</c>.
/// </summary>
public interface IModuleEvent : IBusEvent
{
	string OwnerId { get; }
}
