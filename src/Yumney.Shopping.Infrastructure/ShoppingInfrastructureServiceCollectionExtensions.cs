using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Client;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.Users.Client;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure;

public static class ShoppingInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddYumneyNpgsqlDbContextWithOutbox<ShoppingDbContext>(
			configuration,
			"shoppingdb",
			"__ShoppingMigrationsHistory",
			"wolverine_shopping");
		services.AddYumneyNpgsqlDbContext<ShoppingReadDbContext>(
			configuration,
			"shoppingdb",
			"__ShoppingMigrationsHistory",
			typeof(QueryCountingInterceptor));

		services.AddScoped<IShoppingListEventStore, ShoppingListEventStore>();
		services.AddScoped<IShoppingEventStore, ShoppingEventStore>();
		services.AddScoped<IShoppingLedgerReadModelRepository, ShoppingLedgerReadModelRepository>();
		services.AddScoped<IShoppingListProjectionRepository, ShoppingListProjectionRepository>();
		services.AddScoped<IShoppingListProjectionRebuilder, ShoppingListProjectionRebuilder>();
		services.AddScoped<IIngredientBalanceReadModelRepository, IngredientBalanceReadModelRepository>();
		services.AddScoped<IStaplesProvider, HttpStaplesProvider>();
		services.AddScoped<IRecipeIngredientLookup, HttpRecipeIngredientLookup>();
		services.AddRecipesClient();
		services.AddScoped<IInboxStore, InboxStore<ShoppingDbContext>>();
		services.AddBusEventHandlersFromAssemblyContaining<ShoppingListProjection>();
		services.AddUsersClient();
		services.AddScoped<IShoppingUserDataPurger, ShoppingUserDataPurger>();
		services.AddHealthChecks()
			.AddDbContextCheck<ShoppingDbContext>("shoppingdb", tags: ["ready"])
			.AddDbContextCheck<ShoppingReadDbContext>("shopping-readdb", tags: ["ready"]);

		return services;
	}
}
