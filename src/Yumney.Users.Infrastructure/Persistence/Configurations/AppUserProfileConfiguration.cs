using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Configurations;

internal sealed class AppUserProfileConfiguration : IEntityTypeConfiguration<AppUserProfile>
{
    public void Configure(EntityTypeBuilder<AppUserProfile> entity)
    {
        entity.ToTable("AppUserProfiles");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id)
            .HasConversion(v => v.Value, v => AppUserProfileIdentifier.From(v));

        entity.Property(e => e.KeycloakUserId)
            .HasConversion(v => v.Value, v => KeycloakUserId.From(v))
            .HasMaxLength(KeycloakUserId.MaxLength)
            .IsRequired();

        entity.Property(e => e.DisplayName)
            .HasConversion(v => v.Value, v => DisplayName.From(v))
            .HasMaxLength(DisplayName.MaxLength)
            .IsRequired();

        entity.Property(e => e.PreferredLanguage)
            .HasConversion(v => v.Value, v => PreferredLanguage.From(v))
            .HasMaxLength(PreferredLanguage.MaxLength)
            .IsRequired();

        entity.Property(e => e.PreferredUnitSystem)
            .HasConversion(v => v.Value, v => PreferredUnitSystem.From(v))
            .HasMaxLength(PreferredUnitSystem.MaxLength)
            .IsRequired();

        entity.HasIndex(e => e.KeycloakUserId).IsUnique();
        entity.Ignore(e => e.DomainEvents);
    }
}
