using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanWeekReadItemConfiguration : IEntityTypeConfiguration<MealPlanWeekReadItem>
{
	public void Configure(EntityTypeBuilder<MealPlanWeekReadItem> entity)
	{
		entity.ToTable("MealPlanWeekReadItems");
		entity.HasKey(e => new { e.OwnerId, e.Week });
		entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
		entity.Property(e => e.Week).HasMaxLength(10).IsRequired();
		entity.Property(e => e.IsExtendedMode).IsRequired();
		entity.Property(e => e.LastUpdated).IsRequired();
	}
}
