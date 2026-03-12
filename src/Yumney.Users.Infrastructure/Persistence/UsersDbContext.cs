using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<AppUserProfile> AppUserProfiles => Set<AppUserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserProfile>(entity =>
        {
            entity.ToTable("AppUserProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KeycloakUserId)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new KeycloakUserId(v), KeycloakUserId.MaxLength);
            entity.Property(e => e.DisplayName)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new DisplayName(v), DisplayName.MaxLength);
            entity.Property(e => e.PreferredLanguage)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new PreferredLanguage(v), PreferredLanguage.MaxLength);
            entity.Property(e => e.PreferredUnitSystem)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new PreferredUnitSystem(v), PreferredUnitSystem.MaxLength);
            entity.HasIndex(e => e.KeycloakUserId).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
