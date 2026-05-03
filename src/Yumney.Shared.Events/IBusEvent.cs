namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Common tag carried by anything that flows through <see cref="IEventBus"/>.
/// Two specialisations exist:
/// <see cref="IIntegrationEvent"/> for cross-module contracts and
/// <see cref="IModuleEvent"/> for in-module bus envelopes that wrap a domain event.
/// Use the specialised marker, never <c>IBusEvent</c> directly on a concrete type.
/// </summary>
public interface IBusEvent
{
	Guid EventIdentifier { get; }

	DateTime OccurredOn { get; }
}
