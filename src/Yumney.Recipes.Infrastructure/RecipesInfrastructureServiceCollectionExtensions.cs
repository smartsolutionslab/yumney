using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Persistence;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure;

public static class RecipesInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRecipesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RecipesDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("recipesdb");
            options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__RecipesMigrationsHistory").EnableRetryOnFailure())
                   .AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeFavoriteRepository, RecipeFavoriteRepository>();
        services.AddScoped<IRecipeIngredientProvider, RecipeIngredientProvider>();
        services.AddHealthChecks().AddDbContextCheck<RecipesDbContext>("recipesdb");

        return services;
    }
}
