using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class StoredEventConfigurationBaseTests
{
	[Fact]
	public void Configure_MapsToTheSuppliedTableName()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(StoredEvent))!;

		entityType.GetTableName().Should().Be("TestEvents");
	}

	[Fact]
	public void Configure_PrimaryKey_IsId()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(StoredEvent))!;
		var primaryKey = entityType.FindPrimaryKey()!;

		primaryKey.Properties.Should().ContainSingle()
			.Which.Name.Should().Be(nameof(StoredEvent.Id));
	}

	[Fact]
	public void Configure_EventType_HasMaxLength100AndIsRequired()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(StoredEvent))!;
		var property = entityType.FindProperty(nameof(StoredEvent.EventType))!;

		property.GetMaxLength().Should().Be(100);
		property.IsNullable.Should().BeFalse();
	}

	[Fact]
	public void Configure_RequiredProperties_AreNotNullable()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(StoredEvent))!;

		entityType.FindProperty(nameof(StoredEvent.AggregateId))!.IsNullable.Should().BeFalse();
		entityType.FindProperty(nameof(StoredEvent.EventData))!.IsNullable.Should().BeFalse();
		entityType.FindProperty(nameof(StoredEvent.Version))!.IsNullable.Should().BeFalse();
		entityType.FindProperty(nameof(StoredEvent.OccurredAt))!.IsNullable.Should().BeFalse();
	}

	[Fact]
	public void Configure_UniqueIndex_OverAggregateIdAndVersion()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(StoredEvent))!;

		entityType.GetIndexes().Should().Contain(index =>
			index.IsUnique
			&& index.Properties.Count == 2
			&& index.Properties[0].Name == nameof(StoredEvent.AggregateId)
			&& index.Properties[1].Name == nameof(StoredEvent.Version));
	}

	[Fact]
	public void Configure_NonUniqueIndex_OverOccurredAt()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(StoredEvent))!;

		entityType.GetIndexes().Should().Contain(index =>
			!index.IsUnique
			&& index.Properties.Count == 1
			&& index.Properties[0].Name == nameof(StoredEvent.OccurredAt));
	}

	private static TestContext NewContext()
	{
		var options = new DbContextOptionsBuilder<TestContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new TestContext(options);
	}

	private sealed class TestStoredEventConfiguration : StoredEventConfigurationBase<StoredEvent>
	{
		public TestStoredEventConfiguration()
			: base("TestEvents")
		{
		}
	}

	private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
	{
		public DbSet<StoredEvent> Events => Set<StoredEvent>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfiguration(new TestStoredEventConfiguration());
		}
	}
}
