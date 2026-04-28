using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Application;
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
		services.AddSingleton(_ =>
		{
			var section = configuration.GetSection(ShoppingOptions.SectionName);
			return new ShoppingOptions
			{
				UseProjectionReadModel = bool.TryParse(section["UseProjectionReadModel"], out var v) ? v : true,
			};
		});

		var connectionString = configuration.GetConnectionString("shoppingdb");
		Action<NpgsqlDbContextOptionsBuilder> contextOptions = builder => builder
			.MigrationsHistoryTable("__ShoppingMigrationsHistory")
			.EnableRetryOnFailure();

		services.AddDbContext<ShoppingDbContext>((sp, options) =>
		{
			options
				.UseNpgsql(connectionString, contextOptions)
				.AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>());
		});

		services.AddDbContext<ShoppingReadDbContext>(options =>
		{
			options.UseNpgsql(connectionString, contextOptions);
		});

		services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
		services.AddScoped<IShoppingListEventStore, EfCoreShoppingListEventStore>();
		services.AddScoped<IShoppingUnitOfWork, ShoppingUnitOfWork>();
		services.AddScoped<IShoppingEventStore, EfCoreShoppingEventStore>();
		services.AddScoped<IShoppingListWriter, ShoppingListWriter>();
		services.AddScoped<IShoppingListReadModelRepository, ShoppingListReadModelRepository>();
		services.AddScoped<IShoppingListProjectionRepository, EfCoreShoppingListProjectionRepository>();
		services.AddScoped<ShoppingListProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemAddedIntegrationEvent>, ShoppingListProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemBoughtIntegrationEvent>, ShoppingListProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemConsumedIntegrationEvent>, ShoppingListProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemRemovedIntegrationEvent>, ShoppingListProjectionHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingItemQuantityAdjustedIntegrationEvent>, ShoppingListProjectionHandler>();
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
