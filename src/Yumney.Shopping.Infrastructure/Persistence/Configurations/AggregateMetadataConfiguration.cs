using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

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
