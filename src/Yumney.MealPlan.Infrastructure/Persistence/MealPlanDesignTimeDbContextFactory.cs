using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

public sealed class MealPlanDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MealPlanDbContext>
{
	public MealPlanDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<MealPlanDbContext>();
		optionsBuilder.UseNpgsql(
			"Host=localhost;Database=yumneydb;Username=postgres;Password=postgres",
			x => x.MigrationsHistoryTable("__MealPlanMigrationsHistory"));

		return new MealPlanDbContext(optionsBuilder.Options);
	}
}
