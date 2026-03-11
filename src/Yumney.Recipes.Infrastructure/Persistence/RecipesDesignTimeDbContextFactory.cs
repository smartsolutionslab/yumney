using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipesDesignTimeDbContextFactory : IDesignTimeDbContextFactory<RecipesDbContext>
{
    public RecipesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RecipesDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=yumneydb;Username=postgres;Password=postgres",
            x => x.MigrationsHistoryTable("__RecipesMigrationsHistory"));

        return new RecipesDbContext(optionsBuilder.Options);
    }
}
