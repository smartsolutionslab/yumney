using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

#pragma warning disable SA1649
internal sealed class ShoppingListStoredEventConfiguration : IEntityTypeConfiguration<ShoppingListStoredEvent>
{
	public void Configure(EntityTypeBuilder<ShoppingListStoredEvent> entity)
	{
		entity.ToTable("ShoppingListEvents");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.AggregateId).IsRequired();
		entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
		entity.Property(e => e.EventData).IsRequired();
		entity.Property(e => e.Version).IsRequired();
		entity.Property(e => e.OccurredAt).IsRequired();

		entity.HasIndex(e => new { e.AggregateId, e.Version }).IsUnique();
		entity.HasIndex(e => e.OccurredAt);
	}
}
