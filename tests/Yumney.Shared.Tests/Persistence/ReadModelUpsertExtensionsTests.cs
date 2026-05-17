using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence;

public class ReadModelUpsertExtensionsTests
{
	[Fact]
	public async Task UpsertAsync_RowMissing_InvokesCreateAndMutateAndSaves()
	{
		await using var context = NewContext();
		var id = Guid.NewGuid();

		await context.UpsertAsync<Row>(
			row => row.Id == id,
			() => new Row { Id = id, Name = "fresh" },
			row => row.Count = 1);

		var persisted = await context.Rows.SingleAsync();
		persisted.Id.Should().Be(id);
		persisted.Name.Should().Be("fresh");
		persisted.Count.Should().Be(1);
	}

	[Fact]
	public async Task UpsertAsync_RowExists_OnlyInvokesMutate()
	{
		await using var context = NewContext();
		var id = Guid.NewGuid();
		context.Rows.Add(new Row { Id = id, Name = "existing", Count = 3 });
		await context.SaveChangesAsync();

		var createCalled = false;
		await context.UpsertAsync<Row>(
			row => row.Id == id,
			() =>
			{
				createCalled = true;
				return new Row { Id = id, Name = "should-not-appear" };
			},
			row => row.Count++);

		createCalled.Should().BeFalse();
		var persisted = await context.Rows.SingleAsync();
		persisted.Name.Should().Be("existing");
		persisted.Count.Should().Be(4);
	}

	[Fact]
	public async Task UpdateAsync_RowExists_AppliesMutateAndSaves()
	{
		await using var context = NewContext();
		var id = Guid.NewGuid();
		context.Rows.Add(new Row { Id = id, Name = "before", Count = 0 });
		await context.SaveChangesAsync();

		await context.UpdateAsync<Row>(
			row => row.Id == id,
			row => row.Name = "after");

		var persisted = await context.Rows.SingleAsync();
		persisted.Name.Should().Be("after");
	}

	[Fact]
	public async Task UpdateAsync_RowMissing_IsNoOp()
	{
		await using var context = NewContext();

		await context.UpdateAsync<Row>(
			row => row.Id == Guid.NewGuid(),
			row => row.Name = "should-not-be-set");

		var count = await context.Rows.CountAsync();
		count.Should().Be(0);
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
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public int Count { get; set; }
	}

	private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
	{
		public DbSet<Row> Rows => Set<Row>();
	}
}
