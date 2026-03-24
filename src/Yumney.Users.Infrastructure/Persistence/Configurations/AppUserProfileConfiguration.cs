using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shared.Persistence;
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
            .ConfigureRequiredStringValueObject(v => v.Value, KeycloakUserId.From, KeycloakUserId.MaxLength);

        entity.Property(e => e.DisplayName)
            .ConfigureRequiredStringValueObject(v => v.Value, DisplayName.From, DisplayName.MaxLength);

        entity.Property(e => e.PreferredLanguage)
            .ConfigureRequiredStringValueObject(v => v.Value, PreferredLanguage.From, PreferredLanguage.MaxLength);

        entity.Property(e => e.PreferredUnitSystem)
            .ConfigureRequiredStringValueObject(v => v.Value, PreferredUnitSystem.From, PreferredUnitSystem.MaxLength);

        entity.HasIndex(e => e.KeycloakUserId).IsUnique();
        entity.Ignore(e => e.DomainEvents);
    }
}
