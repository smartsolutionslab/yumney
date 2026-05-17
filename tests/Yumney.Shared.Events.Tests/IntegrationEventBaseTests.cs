using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class IntegrationEventBaseTests
{
	private sealed record FakeIntegrationEvent(string OwnerId) : IntegrationEvent;

	[Fact]
	public void DefaultCtor_AutoStampsNonEmptyEventIdentifier()
	{
		var @event = new FakeIntegrationEvent("user-1");

		@event.EventIdentifier.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public void DefaultCtor_AutoStampsOccurredOnFromUtcNow()
	{
		var @event = new FakeIntegrationEvent("user-1");

		@event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void TwoInstances_HaveDifferentEventIdentifiers()
	{
		var first = new FakeIntegrationEvent("user-1");
		var second = new FakeIntegrationEvent("user-1");

		first.EventIdentifier.Should().NotBe(second.EventIdentifier);
	}

	[Fact]
	public void InitSetters_AcceptIncomingValues()
	{
		var id = Guid.NewGuid();
		var when = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

		var @event = new FakeIntegrationEvent("user-1")
		{
			EventIdentifier = id,
			OccurredOn = when,
		};

		@event.EventIdentifier.Should().Be(id);
		@event.OccurredOn.Should().Be(when);
	}
}
