using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Configurations;

internal sealed class AggregateMetadataConfiguration : IEntityTypeConfiguration<AggregateMetadata>
{
	public void Configure(EntityTypeBuilder<AggregateMetadata> entity)
	{
		entity.ToTable("MealPlanAggregates");
		entity.HasKey(e => e.AggregateId);
		entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
		entity.Property(e => e.Week).HasMaxLength(10).IsRequired();
		entity.HasIndex(e => new { e.OwnerId, e.Week }).IsUnique();
	}
}
