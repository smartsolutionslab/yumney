using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class DomainEventTests
{
	private sealed record TestEvent() : DomainEvent;

	[Fact]
	public void OccurredOn_IsSetToUtcNow()
	{
		var before = DateTime.UtcNow;

		var domainEvent = new TestEvent();

		var after = DateTime.UtcNow;
		domainEvent.OccurredOn.Should().BeOnOrAfter(before);
		domainEvent.OccurredOn.Should().BeOnOrBefore(after);
	}

	[Fact]
	public void OccurredOn_IsUtcKind()
	{
		var domainEvent = new TestEvent();

		domainEvent.OccurredOn.Kind.Should().Be(DateTimeKind.Utc);
	}

	[Fact]
	public void ImplementsIDomainEvent()
	{
		var domainEvent = new TestEvent();

		domainEvent.Should().BeAssignableTo<IDomainEvent>();
	}
}
