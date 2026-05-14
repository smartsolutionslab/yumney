using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListAggregateMetadataConfiguration : IEntityTypeConfiguration<ShoppingListAggregateMetadata>
{
	public void Configure(EntityTypeBuilder<ShoppingListAggregateMetadata> builder)
	{
		builder.ToTable("ShoppingListAggregates");
		builder.HasKey(metadata => metadata.AggregateId);
		builder.Property(metadata => metadata.OwnerId).HasMaxLength(255).IsRequired();
		builder.HasIndex(metadata => metadata.OwnerId);
	}
}
