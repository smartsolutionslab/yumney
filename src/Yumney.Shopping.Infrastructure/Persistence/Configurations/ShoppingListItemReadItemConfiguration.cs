using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListItemReadItemConfiguration : IEntityTypeConfiguration<ShoppingListItemReadItem>
{
	public void Configure(EntityTypeBuilder<ShoppingListItemReadItem> entity)
	{
		entity.ToTable("ShoppingListItemReadItems");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
		entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
		entity.Property(e => e.QuantityUnit).HasMaxLength(50);
		entity.Property(e => e.Category).HasMaxLength(50).IsRequired().HasDefaultValue("other");
		entity.Property(e => e.CreatedAt).IsRequired();
		entity.Property(e => e.LastUpdated).IsRequired();

		entity.HasIndex(e => e.ListId);
		entity.HasIndex(e => new { e.OwnerId, e.ListId });
	}
}
