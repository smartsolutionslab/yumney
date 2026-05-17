using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class EventTypeRegistryTests
{
	public sealed record TypeRegistryFakeEventAlpha() : DomainEvent;

	public sealed record TypeRegistryFakeEventBeta() : DomainEvent;

	public sealed class TypeRegistryNotAnEvent;

	[Fact]
	public void BuildFromAssembly_WithFilter_ReturnsMatchingConcreteEventTypes()
	{
		var map = EventTypeRegistry.BuildFromAssembly(
			typeof(EventTypeRegistryTests).Assembly,
			type => type.Name.StartsWith("TypeRegistryFake", StringComparison.Ordinal));

		map.Should().ContainKey(nameof(TypeRegistryFakeEventAlpha));
		map.Should().ContainKey(nameof(TypeRegistryFakeEventBeta));
		map[nameof(TypeRegistryFakeEventAlpha)].Should().Be<TypeRegistryFakeEventAlpha>();
	}

	[Fact]
	public void BuildFromAssembly_WithFilter_SkipsNonEventTypes()
	{
		var map = EventTypeRegistry.BuildFromAssembly(
			typeof(EventTypeRegistryTests).Assembly,
			type => type.Name == nameof(TypeRegistryNotAnEvent));

		map.Should().BeEmpty();
	}

	[Fact]
	public void BuildFromAssembly_WithFilter_NarrowsToSingleType()
	{
		var map = EventTypeRegistry.BuildFromAssembly(
			typeof(EventTypeRegistryTests).Assembly,
			type => type.Name == nameof(TypeRegistryFakeEventAlpha));

		map.Should().ContainKey(nameof(TypeRegistryFakeEventAlpha));
		map.Should().NotContainKey(nameof(TypeRegistryFakeEventBeta));
	}
}
