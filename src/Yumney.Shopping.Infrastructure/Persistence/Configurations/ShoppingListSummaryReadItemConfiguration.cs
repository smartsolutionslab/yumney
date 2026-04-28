using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListSummaryReadItemConfiguration : IEntityTypeConfiguration<ShoppingListSummaryReadItem>
{
	public void Configure(EntityTypeBuilder<ShoppingListSummaryReadItem> entity)
	{
		entity.ToTable("ShoppingListSummaryReadItems");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
		entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
		entity.Property(e => e.CreatedAt).IsRequired();
		entity.Property(e => e.LastUpdated).IsRequired();

		entity.HasIndex(e => e.OwnerId);
		entity.HasIndex(e => new { e.OwnerId, e.CreatedAt });
		entity.HasIndex(e => new { e.OwnerId, e.Title });
	}
}
