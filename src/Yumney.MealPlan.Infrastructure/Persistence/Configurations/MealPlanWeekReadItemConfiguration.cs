using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanWeekReadItemConfiguration : IEntityTypeConfiguration<MealPlanWeekReadItem>
{
	public void Configure(EntityTypeBuilder<MealPlanWeekReadItem> builder)
	{
		builder.ToTable("MealPlanWeekReadItems");
		builder.HasKey(week => new { week.OwnerId, week.Week });
		builder.Property(week => week.OwnerId).HasMaxLength(255).IsRequired();
		builder.Property(week => week.Week).HasMaxLength(10).IsRequired();
		builder.Property(week => week.IsExtendedMode).IsRequired();
		builder.Property(week => week.LastUpdated).IsRequired();
	}
}
