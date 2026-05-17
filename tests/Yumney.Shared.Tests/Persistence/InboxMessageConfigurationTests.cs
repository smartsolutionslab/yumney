using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence;

public class InboxMessageConfigurationTests
{
	[Fact]
	public void Configure_RegistersCompositePrimaryKey_OverMessageIdAndConsumerName()
	{
		using var context = NewContext();

		var entityType = context.Model.FindEntityType(typeof(InboxMessage))!;
		var primaryKey = entityType.FindPrimaryKey()!;

		primaryKey.Properties.Select(property => property.Name)
			.Should().Equal(nameof(InboxMessage.MessageId), nameof(InboxMessage.ConsumerName));
	}

	[Fact]
	public void Configure_MapsToInboxMessagesTable()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(InboxMessage))!;

		entityType.GetTableName().Should().Be("InboxMessages");
	}

	[Fact]
	public void Configure_ConsumerName_HasMaxLength256AndIsRequired()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(InboxMessage))!;
		var consumerName = entityType.FindProperty(nameof(InboxMessage.ConsumerName))!;

		consumerName.IsNullable.Should().BeFalse();
		consumerName.GetMaxLength().Should().Be(256);
	}

	[Fact]
	public void Configure_ProcessedAt_IsRequired()
	{
		using var context = NewContext();
		var entityType = context.Model.FindEntityType(typeof(InboxMessage))!;
		var processedAt = entityType.FindProperty(nameof(InboxMessage.ProcessedAt))!;

		processedAt.IsNullable.Should().BeFalse();
	}

	private static TestContext NewContext()
	{
		var options = new DbContextOptionsBuilder<TestContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new TestContext(options);
	}

	private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
	{
		public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
		}
	}
}
