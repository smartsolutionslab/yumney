using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Services;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure;

public static class ShoppingInfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("shoppingdb");
		Action<NpgsqlDbContextOptionsBuilder> contextOptions = builder => builder
			.MigrationsHistoryTable("__ShoppingMigrationsHistory")
			.EnableRetryOnFailure();

		services.AddDbContext<ShoppingDbContext>(options =>
		{
			options.UseNpgsql(connectionString, contextOptions);
		});

		services.AddDbContext<ShoppingReadDbContext>(options =>
		{
			options.UseNpgsql(connectionString, contextOptions);
		});

		services.AddScoped<IShoppingListEventStore, EfCoreShoppingListEventStore>();
		services.AddScoped<IShoppingEventStore, EfCoreShoppingEventStore>();
		services.AddScoped<IShoppingListWriter, ShoppingListWriter>();
		services.AddScoped<IShoppingLedgerReadModelRepository, ShoppingLedgerReadModelRepository>();
		services.AddScoped<IShoppingListProjectionRepository, EfCoreShoppingListProjectionRepository>();
		services.AddScoped<IShoppingListProjectionRebuilder, ShoppingListProjectionRebuilder>();
		services.AddScoped<ShoppingLedgerProjectionHandler>();
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
		services.AddScoped<IInboxStore, EfCoreInboxStore<ShoppingDbContext>>();
		services.AddHealthChecks().AddDbContextCheck<ShoppingDbContext>("shoppingdb");

		return services;
	}
}
