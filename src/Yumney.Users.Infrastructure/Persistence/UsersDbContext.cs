using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
	public DbSet<AppUserProfile> AppUserProfiles => Set<AppUserProfile>();

	public DbSet<UserActivity> UserActivities => Set<UserActivity>();

	public DbSet<StaplesList> StaplesLists => Set<StaplesList>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);

		// Wolverine's envelope tables (under the wolverine_users schema) are
		// provisioned at API host startup by AutoBuildMessageStorageOnStartup —
		// not via EF migrations. State-based DeleteAccountCommandHandler still
		// uses the legacy save-then-publish pattern; a follow-up will convert
		// it to publish-then-save (UserAccountDeletedIntegrationEvent is
		// GDPR-critical).
	}
}
