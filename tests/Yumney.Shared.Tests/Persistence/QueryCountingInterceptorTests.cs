using FluentAssertions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence;

/// <summary>
/// Verifies that each overridden hook increments the injected counter.
/// A full EF Core wiring test lives in the Integration.Tests suite where a
/// relational provider is available — these tests are the unit-level guard.
/// </summary>
public class QueryCountingInterceptorTests
{
	private readonly QueryCounter counter = new();
	private readonly QueryCountingInterceptor interceptor;

	public QueryCountingInterceptorTests()
	{
		interceptor = new QueryCountingInterceptor(counter);
	}

	[Fact]
	public void ReaderExecuting_IncrementsCounter()
	{
		interceptor.ReaderExecuting(default!, default!, default);

		counter.Count.Should().Be(1);
	}

	[Fact]
	public async Task ReaderExecutingAsync_IncrementsCounter()
	{
		await interceptor.ReaderExecutingAsync(default!, default!, default, CancellationToken.None);

		counter.Count.Should().Be(1);
	}

	[Fact]
	public void NonQueryExecuting_IncrementsCounter()
	{
		interceptor.NonQueryExecuting(default!, default!, default);

		counter.Count.Should().Be(1);
	}

	[Fact]
	public async Task NonQueryExecutingAsync_IncrementsCounter()
	{
		await interceptor.NonQueryExecutingAsync(default!, default!, default, CancellationToken.None);

		counter.Count.Should().Be(1);
	}

	[Fact]
	public void ScalarExecuting_IncrementsCounter()
	{
		interceptor.ScalarExecuting(default!, default!, default);

		counter.Count.Should().Be(1);
	}

	[Fact]
	public async Task ScalarExecutingAsync_IncrementsCounter()
	{
		await interceptor.ScalarExecutingAsync(default!, default!, default, CancellationToken.None);

		counter.Count.Should().Be(1);
	}

	[Fact]
	public void MixedHookCalls_AccumulateCount()
	{
		interceptor.ReaderExecuting(default!, default!, default);
		interceptor.NonQueryExecuting(default!, default!, default);
		interceptor.ScalarExecuting(default!, default!, default);

		counter.Count.Should().Be(3);
	}
}
