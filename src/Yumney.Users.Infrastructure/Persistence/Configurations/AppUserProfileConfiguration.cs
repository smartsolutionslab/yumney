using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Configurations;

internal sealed class AppUserProfileConfiguration : IEntityTypeConfiguration<AppUserProfile>
{
    public void Configure(EntityTypeBuilder<AppUserProfile> entity)
    {
        entity.ToTable("AppUserProfiles");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id)
            .HasConversion<AppUserProfileIdentifierConverter>();

        entity.Property(e => e.KeycloakUserId)
            .HasConversion<KeycloakUserIdConverter>()
            .HasMaxLength(KeycloakUserId.MaxLength)
            .IsRequired();

        entity.Property(e => e.DisplayName)
            .HasConversion<DisplayNameConverter>()
            .HasMaxLength(DisplayName.MaxLength)
            .IsRequired();

        entity.Property(e => e.PreferredLanguage)
            .HasConversion<PreferredLanguageConverter>()
            .HasMaxLength(PreferredLanguage.MaxLength)
            .IsRequired();

        entity.Property(e => e.PreferredUnitSystem)
            .HasConversion<PreferredUnitSystemConverter>()
            .HasMaxLength(PreferredUnitSystem.MaxLength)
            .IsRequired();

        entity.HasIndex(e => e.KeycloakUserId).IsUnique();
        entity.Ignore(e => e.DomainEvents);
    }
}
