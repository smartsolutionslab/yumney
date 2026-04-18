using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Configurations;

internal sealed class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlan>
{
    public void Configure(EntityTypeBuilder<WeeklyPlan> entity)
    {
        entity.ToTable("WeeklyPlans");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id)
            .HasConversion<WeeklyPlanIdentifierConverter>();

        entity.Property(e => e.Owner)
            .HasConversion<OwnerIdentifierConverter>()
            .HasMaxLength(OwnerIdentifier.MaxLength)
            .IsRequired();

        entity.Property(e => e.Week)
            .HasConversion<WeekIdentifierConverter>()
            .HasMaxLength(10)
            .IsRequired();

        entity.OwnsMany(e => e.Slots, slot =>
        {
            slot.ToTable("MealSlots");
            slot.WithOwner().HasForeignKey("WeeklyPlanId");
            slot.Property(s => s.Id)
                .HasConversion<MealSlotIdentifierConverter>();
            slot.Property(s => s.Day).HasConversion<string>().HasMaxLength(10).IsRequired();
            slot.Property(s => s.MealType).HasConversion<string>().HasMaxLength(10).IsRequired();
            slot.Property(s => s.ContentType).HasConversion<string>().HasMaxLength(10).IsRequired();
            slot.Property(s => s.State).HasConversion<string>().HasMaxLength(10).IsRequired();
            slot.OwnsOne(s => s.Recipe, recipe =>
            {
                recipe.Property(r => r.RecipeIdentifier).HasColumnName("RecipeIdentifier");
                recipe.Property(r => r.Title).HasColumnName("RecipeTitle").HasMaxLength(200);
            });
            slot.Property(s => s.LeftoverLabel).HasConversion<LeftoverLabelConverter>().HasMaxLength(200);
            slot.Property(s => s.Servings).HasConversion<SlotServingsConverter>().IsRequired();
            slot.Property(s => s.FreetextLabel).HasConversion<FreetextLabelConverter>().HasMaxLength(200);
            slot.Property(s => s.LeftoverSourceDay).HasConversion<string>().HasMaxLength(10);
            slot.Property(s => s.LeftoverSourceMealType).HasConversion<string>().HasMaxLength(10);
        });

        entity.Property(e => e.IsExtendedMode).IsRequired();

        entity.HasIndex(e => new { e.Owner, e.Week }).IsUnique();
        entity.Ignore(e => e.DomainEvents);
    }
}
