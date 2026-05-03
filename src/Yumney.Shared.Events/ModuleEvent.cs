namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Abstract record base for in-module bus envelopes. Concrete events sit in
/// the owning module's <c>Infrastructure</c> layer and wrap a Domain event as
/// their <c>Inner</c> property.
/// </summary>
public abstract record ModuleEvent(string OwnerId) : IModuleEvent
{
	public Guid EventIdentifier { get; } = Guid.NewGuid();

	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
