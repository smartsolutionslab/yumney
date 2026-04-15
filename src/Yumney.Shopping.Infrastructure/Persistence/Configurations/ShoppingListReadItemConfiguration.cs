using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

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
