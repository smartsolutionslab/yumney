using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class ModuleEventConventionTests
{
	public sealed record ConventionTestItemAdded(string Name) : DomainEvent;

	public sealed record ConventionTestItemAddedModule(string OwnerId, ConventionTestItemAdded Inner)
		: ModuleEvent(OwnerId);

	// Has a matching ctor shape but it's for a *different* context (Guid instead
	// of string) — should be filtered out when the caller asks for (string).
	public sealed record OtherShapeModuleEvent(Guid Tenant, ConventionTestItemAdded Inner)
		: ModuleEvent(Tenant.ToString());

	[Fact]
	public void BuildMap_RegistersMatchingShapeOnly()
	{
		var map = ModuleEventConvention.BuildMap(
			typeof(ModuleEventConventionTests).Assembly,
			typeof(string));

		map.Should().ContainKey(typeof(ConventionTestItemAdded));
	}

	[Fact]
	public void BuildMap_CompiledFactory_ProducesModuleEventWithContextAndInner()
	{
		var map = ModuleEventConvention.BuildMap(
			typeof(ModuleEventConventionTests).Assembly,
			typeof(string));

		var factory = map[typeof(ConventionTestItemAdded)];
		var domain = new ConventionTestItemAdded("Onion");

		var module = factory([(object)"user-1"], domain);

		module.Should().BeOfType<ConventionTestItemAddedModule>()
			.Which.Should().Match<ConventionTestItemAddedModule>(@event =>
				@event.OwnerId == "user-1" && ReferenceEquals(@event.Inner, domain));
	}

	[Fact]
	public void BuildMap_DifferentContextShape_DoesNotMatch()
	{
		// When asking for (Guid) only OtherShapeModuleEvent matches; the
		// (string) shape used by ConventionTestItemAddedModule is filtered out.
		var map = ModuleEventConvention.BuildMap(
			typeof(ModuleEventConventionTests).Assembly,
			typeof(Guid));

		// At least one event registered for (Guid) shape — the OtherShape variant.
		map.Should().ContainKey(typeof(ConventionTestItemAdded));
	}
}
