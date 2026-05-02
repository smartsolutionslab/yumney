using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure;

public static class RecipesInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddRecipesInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddYumneyNpgsqlDbContext<RecipesDbContext>(
			configuration,
			"recipesdb",
			"__RecipesMigrationsHistory",
			typeof(DomainEventDispatchInterceptor));

		services.AddScoped<IRecipeRepository, RecipeRepository>();
		services.AddScoped<IRecipeFavoriteRepository, RecipeFavoriteRepository>();
		services.AddScoped<IRecipesUnitOfWork, RecipesUnitOfWork>();
		services.AddScoped<IIngredientBalanceProvider, HttpIngredientBalanceProvider>();
		services.AddScoped<IDietaryProfileProvider, HttpDietaryProfileProvider>();
		services.AddYumneyServiceClient("shopping-api");
		services.AddYumneyServiceClient("users-api");
		services.AddHealthChecks().AddDbContextCheck<RecipesDbContext>("recipesdb");

		return services;
	}
}
