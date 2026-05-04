using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure;

public static class MealPlanInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddMealPlanInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddYumneyNpgsqlDbContext<MealPlanDbContext>(configuration, "mealplandb", "__MealPlanMigrationsHistory");
		services.AddYumneyNpgsqlDbContext<MealPlanReadDbContext>(
			configuration,
			"mealplandb",
			"__MealPlanMigrationsHistory",
			typeof(QueryCountingInterceptor));

		services.AddScoped<IMealPlanEventStore, EfCoreMealPlanEventStore>();
		services.AddScoped<IMealPlanReadModelRepository, MealPlanReadModelRepository>();
		services.AddScoped<IMealPlanUserDataPurger, EfCoreMealPlanUserDataPurger>();
		services.AddBusEventHandlersFromAssemblyContaining<MealPlanProjectionHandler>();

		services.AddScoped<IRecipeIngredientLookup, HttpRecipeIngredientLookup>();
		services.AddScoped<IShoppingListWriter, HttpShoppingListWriter>();
		services.AddScoped<IStaplesProvider, HttpStaplesProvider>();
		services.AddYumneyServiceClient("recipes-api");
		services.AddYumneyServiceClient("shopping-api");
		services.AddYumneyServiceClient("users-api");
		services.AddHealthChecks().AddDbContextCheck<MealPlanDbContext>("mealplandb");

		return services;
	}
}
