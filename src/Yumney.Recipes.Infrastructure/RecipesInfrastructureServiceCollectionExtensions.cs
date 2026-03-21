using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Persistence;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure;

public static class RecipesInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRecipesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RecipesDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("recipesdb"),
                    x => x.MigrationsHistoryTable("__RecipesMigrationsHistory"))
                .AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddScoped<IRecipeRepository, RecipeRepository>();

        return services;
    }
}
