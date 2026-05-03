using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure;

public static class ShoppingInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddYumneyNpgsqlDbContext<ShoppingDbContext>(configuration, "shoppingdb", "__ShoppingMigrationsHistory");
		services.AddYumneyNpgsqlDbContext<ShoppingReadDbContext>(
			configuration,
			"shoppingdb",
			"__ShoppingMigrationsHistory",
			typeof(QueryCountingInterceptor));

		services.AddScoped<IShoppingListEventStore, EfCoreShoppingListEventStore>();
		services.AddScoped<IShoppingEventStore, EfCoreShoppingEventStore>();
		services.AddScoped<IShoppingLedgerReadModelRepository, ShoppingLedgerReadModelRepository>();
		services.AddScoped<IShoppingListProjectionRepository, EfCoreShoppingListProjectionRepository>();
		services.AddScoped<IShoppingListProjectionRebuilder, ShoppingListProjectionRebuilder>();
		services.AddScoped<IIngredientBalanceReadModelRepository, IngredientBalanceReadModelRepository>();
		services.AddScoped<IStaplesProvider, HttpStaplesProvider>();
		services.AddScoped<IRecipeIngredientLookup, HttpRecipeIngredientLookup>();
		services.AddYumneyServiceClient("recipes-api");
		services.AddScoped<IInboxStore, EfCoreInboxStore<ShoppingDbContext>>();
		services.AddIntegrationEventHandlersFromAssemblyContaining<ShoppingListProjection>();
		services.AddYumneyServiceClient("users-api");
		services.AddHealthChecks().AddDbContextCheck<ShoppingDbContext>("shoppingdb");

		return services;
	}
}
