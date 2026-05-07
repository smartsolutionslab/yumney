using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public abstract class StoredEventConfigurationBase<TStoredEvent>(string tableName) : IEntityTypeConfiguration<TStoredEvent>
	where TStoredEvent : class, IStoredEvent
{
	public void Configure(EntityTypeBuilder<TStoredEvent> builder)
	{
		builder.ToTable(tableName);
		builder.HasKey(stored => stored.Id);
		builder.Property(stored => stored.AggregateId).IsRequired();
		builder.Property(stored => stored.EventType).HasMaxLength(100).IsRequired();
		builder.Property(stored => stored.EventData).IsRequired();
		builder.Property(stored => stored.Version).IsRequired();
		builder.Property(stored => stored.OccurredAt).IsRequired();

		builder.HasIndex(stored => new { stored.AggregateId, stored.Version }).IsUnique();
		builder.HasIndex(stored => stored.OccurredAt);
	}
}
