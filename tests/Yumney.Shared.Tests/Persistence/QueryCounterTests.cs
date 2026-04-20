using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence;

public class QueryCounterTests
{
	[Fact]
	public void Count_Initially_ReturnsZero()
	{
		var counter = new QueryCounter();

		counter.Count.Should().Be(0);
	}

	[Fact]
	public void Increment_TwoTimes_CountIsTwo()
	{
		var counter = new QueryCounter();

		counter.Increment();
		counter.Increment();

		counter.Count.Should().Be(2);
	}

	[Fact]
	public void Reset_AfterIncrement_CountIsZero()
	{
		var counter = new QueryCounter();
		counter.Increment();
		counter.Increment();

		counter.Reset();

		counter.Count.Should().Be(0);
	}

	[Fact]
	public void Increment_ConcurrentCalls_CountsAll()
	{
		var counter = new QueryCounter();

		Parallel.For(0, 1000, _ => counter.Increment());

		counter.Count.Should().Be(1000);
	}
}
