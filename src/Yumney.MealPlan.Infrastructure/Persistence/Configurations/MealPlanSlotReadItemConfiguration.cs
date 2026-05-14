using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanSlotReadItemConfiguration : IEntityTypeConfiguration<MealPlanSlotReadItem>
{
	public void Configure(EntityTypeBuilder<MealPlanSlotReadItem> builder)
	{
		builder.ToTable("MealPlanSlotReadItems");
		builder.HasKey(slot => slot.Id);
		builder.Property(slot => slot.OwnerId).HasMaxLength(255).IsRequired();
		builder.Property(slot => slot.Week).HasMaxLength(10).IsRequired();
		builder.Property(slot => slot.Day).HasMaxLength(10).IsRequired();
		builder.Property(slot => slot.MealType).HasMaxLength(10).IsRequired();
		builder.Property(slot => slot.ContentType).HasMaxLength(10).IsRequired();
		builder.Property(slot => slot.State).HasMaxLength(10).IsRequired();
		builder.Property(slot => slot.RecipeTitle).HasMaxLength(200);
		builder.Property(slot => slot.FreetextLabel).HasMaxLength(200);
		builder.Property(slot => slot.LeftoverLabel).HasMaxLength(200);
		builder.Property(slot => slot.LeftoverSourceDay).HasMaxLength(10);
		builder.Property(slot => slot.LeftoverSourceMealType).HasMaxLength(10);
		builder.Property(slot => slot.LastUpdated).IsRequired();

		builder.HasIndex(slot => new { slot.OwnerId, slot.Week, slot.Day, slot.MealType }).IsUnique();
		builder.HasIndex(slot => new { slot.OwnerId, slot.Week });
	}
}
