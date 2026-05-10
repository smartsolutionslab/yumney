using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;
using SmartSolutionsLab.Yumney.Shared.Events;
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

		// State-based handlers stage cross-module integration events on the typed
		// outbox (last AddScoped<IEventBus> wins, so this overrides the default
		// WolverineEventBus from AddYumneyDefaults). RecipesUnitOfWork.SaveChangesAsync
		// calls outbox.FlushOutgoingMessagesAsync after persisting the entity changes,
		// which is what actually delivers the staged messages — without the flush
		// they sit in the outbox table waiting on the polling relay.
		services.AddScoped<IEventBus, WolverineOutboxEventBus<RecipesDbContext>>();

		services.AddScoped<RecipesUnitOfWork>();
		services.AddScoped<IRecipesUnitOfWork>(sp => sp.GetRequiredService<RecipesUnitOfWork>());
		services.AddScoped<IRecipeRepository>(sp => sp.GetRequiredService<RecipesUnitOfWork>().Recipes);
		services.AddScoped<IRecipeFavoriteRepository>(sp => sp.GetRequiredService<RecipesUnitOfWork>().Favorites);
		services.AddScoped<IRecipesUserDataPurger, RecipesUserDataPurger>();
		services.AddScoped<IIngredientBalanceProvider, HttpIngredientBalanceProvider>();
		services.AddScoped<IDietaryProfileProvider, HttpDietaryProfileProvider>();
		services.AddScoped<IWeeklyPlanLookup, HttpWeeklyPlanLookup>();
		services.AddScoped<IRecipeViewTracker, CachedRecipeViewTracker>();
		services.AddShoppingClient();
		services.AddUsersClient();
		services.AddMealPlanClient();
		services.AddHealthChecks().AddDbContextCheck<RecipesDbContext>("recipesdb");

		return services;
	}
}
