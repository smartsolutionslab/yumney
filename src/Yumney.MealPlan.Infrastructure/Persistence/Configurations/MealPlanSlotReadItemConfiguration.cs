using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanSlotReadItemConfiguration : IEntityTypeConfiguration<MealPlanSlotReadItem>
{
	public void Configure(EntityTypeBuilder<MealPlanSlotReadItem> entity)
	{
		entity.ToTable("MealPlanSlotReadItems");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
		entity.Property(e => e.Week).HasMaxLength(10).IsRequired();
		entity.Property(e => e.Day).HasMaxLength(10).IsRequired();
		entity.Property(e => e.MealType).HasMaxLength(10).IsRequired();
		entity.Property(e => e.ContentType).HasMaxLength(10).IsRequired();
		entity.Property(e => e.State).HasMaxLength(10).IsRequired();
		entity.Property(e => e.RecipeTitle).HasMaxLength(200);
		entity.Property(e => e.FreetextLabel).HasMaxLength(200);
		entity.Property(e => e.LeftoverLabel).HasMaxLength(200);
		entity.Property(e => e.LeftoverSourceDay).HasMaxLength(10);
		entity.Property(e => e.LeftoverSourceMealType).HasMaxLength(10);
		entity.Property(e => e.LastUpdated).IsRequired();

		entity.HasIndex(e => new { e.OwnerId, e.Week, e.Day, e.MealType }).IsUnique();
		entity.HasIndex(e => new { e.OwnerId, e.Week });
	}
}
