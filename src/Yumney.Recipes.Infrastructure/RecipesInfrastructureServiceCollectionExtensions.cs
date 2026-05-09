using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Client;
using SmartSolutionsLab.Yumney.Users.Client;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure;

public static class RecipesInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddRecipesInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddYumneyNpgsqlDbContextWithOutbox<RecipesDbContext>(
			configuration,
			"recipesdb",
			"__RecipesMigrationsHistory",
			"wolverine_recipes",
			typeof(DomainEventDispatchInterceptor));

		// State-based handlers (SaveRecipe / DeleteRecipe / TrackRecipeCooked) keep
		// the default WolverineEventBus from AddYumneyDefaults. Wolverine's typed
		// IDbContextOutbox<T> only flushes captured messages when the handler
		// itself calls outbox.SaveChangesAndFlushMessagesAsync — a plain
		// DbContext.SaveChangesAsync stages but never delivers, so cross-module
		// integration events (e.g. RecipeDeletedIntegrationEvent) silently
		// disappear. Wolverine's PersistMessagesWithPostgresql still gives the
		// regular bus durable at-least-once delivery; closing the strict
		// publish-before-save dual-write hole is a follow-up that needs the
		// handlers to use the outbox API directly.
		services.AddScoped<RecipesUnitOfWork>();
		services.AddScoped<IRecipesUnitOfWork>(sp => sp.GetRequiredService<RecipesUnitOfWork>());
		services.AddScoped<IRecipeRepository>(sp => sp.GetRequiredService<RecipesUnitOfWork>().Recipes);
		services.AddScoped<IRecipeFavoriteRepository>(sp => sp.GetRequiredService<RecipesUnitOfWork>().Favorites);
		services.AddScoped<IRecipesUserDataPurger, RecipesUserDataPurger>();
		services.AddScoped<IIngredientBalanceProvider, HttpIngredientBalanceProvider>();
		services.AddScoped<IDietaryProfileProvider, HttpDietaryProfileProvider>();
		services.AddScoped<IRecipeViewTracker, CachedRecipeViewTracker>();
		services.AddShoppingClient();
		services.AddUsersClient();
		services.AddHealthChecks().AddDbContextCheck<RecipesDbContext>("recipesdb");

		return services;
	}
}
