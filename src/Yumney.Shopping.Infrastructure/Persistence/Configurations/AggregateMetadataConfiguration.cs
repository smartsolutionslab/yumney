using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class AggregateMetadataConfiguration : IEntityTypeConfiguration<AggregateMetadata>
{
	public void Configure(EntityTypeBuilder<AggregateMetadata> builder)
	{
		builder.ToTable("ShoppingAggregates");
		builder.HasKey(metadata => metadata.AggregateId);
		builder.Property(metadata => metadata.OwnerId).HasMaxLength(255).IsRequired();
		builder.HasIndex(metadata => metadata.OwnerId).IsUnique();
	}
}
