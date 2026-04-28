using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

public sealed class MealPlanReadDbContext(DbContextOptions<MealPlanReadDbContext> options) : DbContext(options)
{
	public DbSet<MealPlanWeekReadItem> MealPlanWeekReadItems => Set<MealPlanWeekReadItem>();

	public DbSet<MealPlanSlotReadItem> MealPlanSlotReadItems => Set<MealPlanSlotReadItem>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(MealPlanDbContext).Assembly);
	}
}
