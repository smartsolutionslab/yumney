using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;
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
		var connectionString = configuration.GetConnectionString("shoppingdb");
		Action<NpgsqlDbContextOptionsBuilder> contextOptions = builder => builder
			.MigrationsHistoryTable("__ShoppingMigrationsHistory")
			.EnableRetryOnFailure();

		services.AddQueryCounting();

		services.AddDbContext<ShoppingDbContext>(options =>
		{
			options.UseNpgsql(connectionString, contextOptions);
		});

		services.AddDbContext<ShoppingReadDbContext>((sp, options) =>
		{
			options
				.UseNpgsql(connectionString, contextOptions)
				.AddInterceptors(sp.GetRequiredService<QueryCountingInterceptor>());
		});

		services.AddScoped<IShoppingListEventStore, EfCoreShoppingListEventStore>();
		services.AddScoped<IShoppingEventStore, EfCoreShoppingEventStore>();
		services.AddScoped<IShoppingLedgerReadModelRepository, ShoppingLedgerReadModelRepository>();
		services.AddScoped<IShoppingListProjectionRepository, EfCoreShoppingListProjectionRepository>();
		services.AddScoped<IShoppingListProjectionRebuilder, ShoppingListProjectionRebuilder>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemAddedIntegrationEvent>, ShoppingLedgerProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemBoughtIntegrationEvent>, ShoppingLedgerProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemConsumedIntegrationEvent>, ShoppingLedgerProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemRemovedIntegrationEvent>, ShoppingLedgerProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemQuantityAdjustedIntegrationEvent>, ShoppingLedgerProjectionHandler>();
		services.AddScoped<ShoppingListProjection>();
		services.AddScoped<IIntegrationEventHandler<ShoppingListCreatedIntegrationEvent>, ShoppingListProjection>();
		services.AddScoped<IIntegrationEventHandler<ListItemAddedIntegrationEvent>, ShoppingListProjection>();
		services.AddScoped<IIntegrationEventHandler<ListItemCheckedIntegrationEvent>, ShoppingListProjection>();
		services.AddScoped<IIntegrationEventHandler<ListItemUncheckedIntegrationEvent>, ShoppingListProjection>();
		services.AddScoped<IIntegrationEventHandler<AllItemsCheckedIntegrationEvent>, ShoppingListProjection>();
		services.AddScoped<IIntegrationEventHandler<AllItemsUncheckedIntegrationEvent>, ShoppingListProjection>();
		services.AddScoped<IIntegrationEventHandler<RecipeReferenceClearedIntegrationEvent>, ShoppingListProjection>();
		services.AddScoped<IIntegrationEventHandler<RecipeDeletedIntegrationEvent>, RecipeDeletedHandler>();
		services.AddScoped<IIntegrationEventHandler<MealConfirmedIntegrationEvent>, MealConfirmedHandler>();
		services.AddScoped<IngredientBalanceProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemBoughtIntegrationEvent>, IngredientBalanceProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemConsumedIntegrationEvent>, IngredientBalanceProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemRemovedIntegrationEvent>, IngredientBalanceProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemUndoBoughtIntegrationEvent>, IngredientBalanceProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemAddedAsAtHomeIntegrationEvent>, IngredientBalanceProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemMarkedAsFrozenIntegrationEvent>, IngredientBalanceProjectionHandler>();
		services.AddScoped<IIngredientBalanceReadModelRepository, IngredientBalanceReadModelRepository>();
		services.AddScoped<IStaplesProvider, HttpStaplesProvider>();
		services.AddTransient<AuthTokenDelegatingHandler>();
		services.AddHttpClient("users-api", client => client.BaseAddress = new Uri("http://users-api"))
			.AddHttpMessageHandler(sp => sp.GetRequiredService<AuthTokenDelegatingHandler>());
		services.AddScoped<IInboxStore, EfCoreInboxStore<ShoppingDbContext>>();
		services.TryAddSingleton(TimeProvider.System);
		services.AddHealthChecks().AddDbContextCheck<ShoppingDbContext>("shoppingdb");

		return services;
	}
}
