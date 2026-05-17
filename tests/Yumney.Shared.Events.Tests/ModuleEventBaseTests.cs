using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class ModuleEventBaseTests
{
	private sealed record FakeModuleEvent(string OwnerId, string Payload) : ModuleEvent(OwnerId);

	[Fact]
	public void DefaultCtor_StampsOwnerAndAutoFields()
	{
		var @event = new FakeModuleEvent("user-1", "hello");

		@event.OwnerId.Should().Be("user-1");
		@event.Payload.Should().Be("hello");
		@event.EventIdentifier.Should().NotBe(Guid.Empty);
		@event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void InitSetters_AcceptIncomingValues()
	{
		var id = Guid.NewGuid();
		var when = new DateTime(2026, 5, 17, 8, 0, 0, DateTimeKind.Utc);

		var @event = new FakeModuleEvent("user-1", "p")
		{
			EventIdentifier = id,
			OccurredOn = when,
		};

		@event.EventIdentifier.Should().Be(id);
		@event.OccurredOn.Should().Be(when);
	}

	[Fact]
	public void TwoInstances_HaveDifferentEventIdentifiers()
	{
		var first = new FakeModuleEvent("user-1", "p");
		var second = new FakeModuleEvent("user-1", "p");

		first.EventIdentifier.Should().NotBe(second.EventIdentifier);
	}
}
