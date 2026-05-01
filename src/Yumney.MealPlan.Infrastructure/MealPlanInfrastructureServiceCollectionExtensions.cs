using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Services;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure;

public static class MealPlanInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddMealPlanInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("mealplandb");
		Action<NpgsqlDbContextOptionsBuilder> contextOptions = builder => builder
			.MigrationsHistoryTable("__MealPlanMigrationsHistory")
			.EnableRetryOnFailure();

		services.AddDbContext<MealPlanDbContext>((_, options) =>
		{
			options.UseNpgsql(connectionString, contextOptions);
		});

		services.AddDbContext<MealPlanReadDbContext>(options =>
		{
			options.UseNpgsql(connectionString, contextOptions);
		});

		services.AddScoped<IMealPlanEventStore, EfCoreMealPlanEventStore>();
		services.AddScoped<IMealPlanReadModelRepository, MealPlanReadModelRepository>();
		services.AddScoped<IIntegrationEventHandler<WeeklyPlanCreatedIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ExtendedModeEnabledIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ExtendedModeDisabledIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<RecipeAssignedIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<MealSetAsFreetextIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<LeftoverAssignedIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<MealSlotClearedIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ServingsAdjustedIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<MealMarkedAsCookedIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<MealMarkedAsSkippedIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<MealResetToPlannedIntegrationEvent>, MealPlanProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<MealSlotsSwappedIntegrationEvent>, MealPlanProjectionHandler>();

		services.AddScoped<IRecipeIngredientLookup, HttpRecipeIngredientLookup>();
		services.AddScoped<IShoppingListWriter, HttpShoppingListWriter>();
		services.AddScoped<IStaplesProvider, HttpStaplesProvider>();
		services.AddTransient<AuthTokenDelegatingHandler>();
		services.AddHttpClient("recipes-api", client => client.BaseAddress = new Uri("http://recipes-api"))
			.AddHttpMessageHandler(sp => sp.GetRequiredService<AuthTokenDelegatingHandler>());
		services.AddHttpClient("shopping-api", client => client.BaseAddress = new Uri("http://shopping-api"))
			.AddHttpMessageHandler(sp => sp.GetRequiredService<AuthTokenDelegatingHandler>());
		services.AddHttpClient("users-api", client => client.BaseAddress = new Uri("http://users-api"))
			.AddHttpMessageHandler(sp => sp.GetRequiredService<AuthTokenDelegatingHandler>());
		services.AddHealthChecks().AddDbContextCheck<MealPlanDbContext>("mealplandb");

		return services;
	}
}
