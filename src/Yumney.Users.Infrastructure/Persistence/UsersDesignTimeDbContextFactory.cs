using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class UsersDesignTimeDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
	public UsersDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<UsersDbContext>();
		optionsBuilder.UseNpgsql(
			"Host=localhost;Database=yumneydb;Username=postgres;Password=postgres",
			x => x.MigrationsHistoryTable("__UsersMigrationsHistory"));

		return new UsersDbContext(optionsBuilder.Options);
	}
}
