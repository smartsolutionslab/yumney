using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingReadDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShoppingReadDbContext>
{
	public ShoppingReadDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<ShoppingReadDbContext>();
		optionsBuilder.UseNpgsql(
			"Host=localhost;Database=yumneydb;Username=postgres;Password=postgres",
			x => x.MigrationsHistoryTable("__ShoppingMigrationsHistory"));

		return new ShoppingReadDbContext(optionsBuilder.Options);
	}
}
