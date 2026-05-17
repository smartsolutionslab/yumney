using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class EventSourcedAggregateTests
{
	private sealed record CounterIncremented(int Step) : DomainEvent;

	private sealed record CounterReset() : DomainEvent;

	private sealed class Counter : EventSourcedAggregate<Guid>
	{
		public int Total { get; private set; }

		public int Resets { get; private set; }

		public Counter(Guid id)
		{
			Identifier = id;
			On<CounterIncremented>(@event => Total += @event.Step);
			On<CounterReset>(_ =>
			{
				Total = 0;
				Resets++;
			});
		}

		public void Increment(int step) => RaiseEvent(new CounterIncremented(step));

		public void Reset() => RaiseEvent(new CounterReset());

		public void Replay(IEnumerable<IDomainEvent> history, AggregateVersion? startVersion = null) =>
			LoadFromHistory(history, startVersion);
	}

	[Fact]
	public void NewAggregate_HasZeroVersionAndNoUncommitted()
	{
		var counter = new Counter(Guid.NewGuid());

		counter.Version.Value.Should().Be(0);
		counter.UncommittedEvents.Should().BeEmpty();
	}

	[Fact]
	public void RaiseEvent_AppliesHandlerAndBuffersEvent()
	{
		var counter = new Counter(Guid.NewGuid());

		counter.Increment(5);

		counter.Total.Should().Be(5);
		counter.Version.Value.Should().Be(1);
		counter.UncommittedEvents.Should().ContainSingle();
	}

	[Fact]
	public void RaiseEvent_TwoEvents_AdvancesVersionTwice()
	{
		var counter = new Counter(Guid.NewGuid());

		counter.Increment(3);
		counter.Increment(2);

		counter.Total.Should().Be(5);
		counter.Version.Value.Should().Be(2);
		counter.UncommittedEvents.Should().HaveCount(2);
	}

	[Fact]
	public void MarkCommitted_ClearsUncommittedBuffer_PreservesVersion()
	{
		var counter = new Counter(Guid.NewGuid());
		counter.Increment(1);

		counter.MarkCommitted();

		counter.UncommittedEvents.Should().BeEmpty();
		counter.Version.Value.Should().Be(1);
	}

	[Fact]
	public void LoadFromHistory_ReplaysWithoutBuffering()
	{
		var counter = new Counter(Guid.NewGuid());

		counter.Replay([new CounterIncremented(2), new CounterIncremented(3)]);

		counter.Total.Should().Be(5);
		counter.Version.Value.Should().Be(2);
		counter.UncommittedEvents.Should().BeEmpty();
	}

	[Fact]
	public void LoadFromHistory_HonoursStartVersion()
	{
		var counter = new Counter(Guid.NewGuid());

		counter.Replay([new CounterReset()], startVersion: AggregateVersion.From(7));

		counter.Resets.Should().Be(1);
		counter.Version.Value.Should().Be(8);
	}

	[Fact]
	public void EventWithoutHandler_IsIgnored()
	{
		var counter = new Counter(Guid.NewGuid());

		counter.Replay([new UnhandledEvent()]);

		counter.Total.Should().Be(0);
		counter.Version.Value.Should().Be(1);
	}

	private sealed record UnhandledEvent() : DomainEvent;
}
