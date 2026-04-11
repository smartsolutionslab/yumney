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
            slot.Property(s => s.RecipeIdentifier);
            slot.Property(s => s.RecipeTitle).HasMaxLength(200);
            slot.Property(s => s.Servings).IsRequired();
        });

        entity.HasIndex(e => new { e.Owner, e.Week }).IsUnique();
        entity.Ignore(e => e.DomainEvents);
    }
}
