using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShoppingDbContext>
{
    public ShoppingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShoppingDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=yumneydb;Username=postgres;Password=postgres",
            x => x.MigrationsHistoryTable("__ShoppingMigrationsHistory"));

        return new ShoppingDbContext(optionsBuilder.Options);
    }
}
