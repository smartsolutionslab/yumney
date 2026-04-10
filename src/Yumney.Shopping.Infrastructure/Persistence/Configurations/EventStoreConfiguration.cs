using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

#pragma warning disable SA1649
internal sealed class StoredEventConfiguration : IEntityTypeConfiguration<StoredEvent>
{
    public void Configure(EntityTypeBuilder<StoredEvent> entity)
    {
        entity.ToTable("ShoppingEvents");
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

internal sealed class StoredSnapshotConfiguration : IEntityTypeConfiguration<StoredSnapshot>
{
    public void Configure(EntityTypeBuilder<StoredSnapshot> entity)
    {
        entity.ToTable("ShoppingSnapshots");
        entity.HasKey(e => e.AggregateId);
        entity.Property(e => e.State).IsRequired();
        entity.Property(e => e.Version).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();
    }
}

internal sealed class AggregateMetadataConfiguration : IEntityTypeConfiguration<AggregateMetadata>
{
    public void Configure(EntityTypeBuilder<AggregateMetadata> entity)
    {
        entity.ToTable("ShoppingAggregates");
        entity.HasKey(e => e.AggregateId);
        entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
        entity.HasIndex(e => e.OwnerId).IsUnique();
    }
}

internal sealed class ShoppingListReadItemConfiguration : IEntityTypeConfiguration<ShoppingListReadItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListReadItem> entity)
    {
        entity.ToTable("ShoppingListReadItems");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
        entity.Property(e => e.ItemName).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Unit).HasMaxLength(20);
        entity.Property(e => e.Category).HasMaxLength(30).IsRequired();
        entity.Property(e => e.SourcesJson).HasColumnType("jsonb");
        entity.Property(e => e.LastUpdated).IsRequired();

        entity.HasIndex(e => e.OwnerId);
        entity.HasIndex(e => new { e.OwnerId, e.ItemName, e.Unit });
    }
}
