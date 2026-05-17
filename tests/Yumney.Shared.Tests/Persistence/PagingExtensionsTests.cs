using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence;

public class PagingExtensionsTests
{
	[Fact]
	public async Task ToPagedListAsync_FirstPage_ReturnsLeadingSliceAndTotal()
	{
		await using var context = NewContext();
		for (var order = 0; order < 25; order++)
		{
			context.Rows.Add(new Row { Id = Guid.NewGuid(), Order = order });
		}

		await context.SaveChangesAsync();

		var (items, total) = await context.Rows.OrderBy(row => row.Order)
			.ToPagedListAsync(PagingOptions.From(1, 10));

		total.Value.Should().Be(25);
		items.Should().HaveCount(10);
		items[0].Order.Should().Be(0);
		items[9].Order.Should().Be(9);
	}

	[Fact]
	public async Task ToPagedListAsync_LaterPage_SkipsPriorPages()
	{
		await using var context = NewContext();
		for (var order = 0; order < 25; order++)
		{
			context.Rows.Add(new Row { Id = Guid.NewGuid(), Order = order });
		}

		await context.SaveChangesAsync();

		var (items, total) = await context.Rows.OrderBy(row => row.Order)
			.ToPagedListAsync(PagingOptions.From(3, 10));

		total.Value.Should().Be(25);
		items.Should().HaveCount(5);
		items[0].Order.Should().Be(20);
	}

	[Fact]
	public async Task ToPagedListAsync_EmptyTable_ReturnsEmptyAndZeroTotal()
	{
		await using var context = NewContext();

		var (items, total) = await context.Rows.ToPagedListAsync(PagingOptions.From(1, 10));

		total.Value.Should().Be(0);
		items.Should().BeEmpty();
	}

	private static TestContext NewContext()
	{
		var options = new DbContextOptionsBuilder<TestContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new TestContext(options);
	}

	private sealed class Row
	{
		public Guid Id { get; init; }

		public int Order { get; init; }
	}

	private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
	{
		public DbSet<Row> Rows => Set<Row>();
	}
}
