using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Configurations;

internal sealed class AggregateMetadataConfiguration : IEntityTypeConfiguration<AggregateMetadata>
{
	public void Configure(EntityTypeBuilder<AggregateMetadata> builder)
	{
		builder.ToTable("MealPlanAggregates");
		builder.HasKey(metadata => metadata.AggregateId);
		builder.Property(metadata => metadata.OwnerId).HasMaxLength(255).IsRequired();
		builder.Property(metadata => metadata.Week).HasMaxLength(10).IsRequired();
		builder.HasIndex(metadata => new { metadata.OwnerId, metadata.Week }).IsUnique();
	}
}
