using Microsoft.EntityFrameworkCore;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<AppUserProfile> AppUserProfiles => Set<AppUserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserProfile>(entity =>
        {
            entity.ToTable("AppUserProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KeycloakUserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PreferredLanguage).HasMaxLength(10).IsRequired();
            entity.Property(e => e.PreferredUnitSystem).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.KeycloakUserId).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
