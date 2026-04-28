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
	}
}
