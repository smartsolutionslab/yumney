using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

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
