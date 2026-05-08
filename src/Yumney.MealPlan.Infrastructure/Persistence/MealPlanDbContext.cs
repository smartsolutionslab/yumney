using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

public sealed class MealPlanDbContext(DbContextOptions<MealPlanDbContext> options) : DbContext(options)
{
	public DbSet<StoredEvent> MealPlanEvents => Set<StoredEvent>();

	public DbSet<AggregateMetadata> MealPlanAggregates => Set<AggregateMetadata>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(MealPlanDbContext).Assembly);

		// Wolverine's envelope tables (under the wolverine_mealplan schema) are
		// provisioned at API host startup by AutoBuildMessageStorageOnStartup —
		// not via EF migrations — because Wolverine.EntityFrameworkCore marks
		// those tables ExcludeFromMigrations() unconditionally.
	}
}
