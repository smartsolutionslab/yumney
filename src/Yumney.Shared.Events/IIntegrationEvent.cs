namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Marker for a cross-module integration event. These are the public contracts
/// between modules — fields must be primitive / shared types only, never a
/// module's Domain types. They live in <c>Yumney.Shared.Events.CrossModule</c>.
/// In-module bus envelopes use <see cref="IModuleEvent"/> instead.
/// </summary>
public interface IIntegrationEvent : IBusEvent
{
}
