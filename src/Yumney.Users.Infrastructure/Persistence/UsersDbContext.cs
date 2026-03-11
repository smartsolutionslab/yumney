using Microsoft.EntityFrameworkCore;
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
                .ConfigureRequiredStringValueObject(v => v.Value, v => new KeycloakUserId(v), 255);
            entity.Property(e => e.DisplayName)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new DisplayName(v), 200);
            entity.Property(e => e.PreferredLanguage)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new PreferredLanguage(v), 10);
            entity.Property(e => e.PreferredUnitSystem)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new PreferredUnitSystem(v), 20);
            entity.HasIndex(e => e.KeycloakUserId).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
