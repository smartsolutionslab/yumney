using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

public sealed class MealPlanReadDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MealPlanReadDbContext>
{
	public MealPlanReadDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<MealPlanReadDbContext>();
		optionsBuilder.UseNpgsql(
			"Host=localhost;Database=yumneydb;Username=postgres;Password=postgres",
			x => x.MigrationsHistoryTable("__MealPlanMigrationsHistory"));

		return new MealPlanReadDbContext(optionsBuilder.Options);
	}
}
