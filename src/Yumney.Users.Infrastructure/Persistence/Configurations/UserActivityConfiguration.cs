using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Configurations;

internal sealed class UserActivityConfiguration : IEntityTypeConfiguration<UserActivity>
{
    public void Configure(EntityTypeBuilder<UserActivity> entity)
    {
        entity.ToTable("UserActivities");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id)
            .HasConversion<UserActivityIdentifierConverter>();

        entity.Property(e => e.Owner)
            .HasConversion<UserActivityOwnerIdentifierConverter>()
            .HasMaxLength(OwnerIdentifier.MaxLength)
            .IsRequired();

        entity.Property(e => e.Type)
            .HasConversion<ActivityTypeConverter>()
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(e => e.RecipeIdentifier);

        entity.Property(e => e.RecipeTitle)
            .HasMaxLength(200);

        entity.Property(e => e.OccurredAt)
            .IsRequired();

        entity.HasIndex(e => new { e.Owner, e.OccurredAt })
            .IsDescending(false, true);
    }
}
