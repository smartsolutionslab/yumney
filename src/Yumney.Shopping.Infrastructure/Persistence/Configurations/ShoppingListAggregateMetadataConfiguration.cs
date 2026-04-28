using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListAggregateMetadataConfiguration : IEntityTypeConfiguration<ShoppingListAggregateMetadata>
{
	public void Configure(EntityTypeBuilder<ShoppingListAggregateMetadata> entity)
	{
		entity.ToTable("ShoppingListAggregates");
		entity.HasKey(e => e.AggregateId);
		entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
		entity.HasIndex(e => e.OwnerId);
	}
}
