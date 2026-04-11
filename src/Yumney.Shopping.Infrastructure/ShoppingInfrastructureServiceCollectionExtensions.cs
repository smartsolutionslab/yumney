using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure;

public static class ShoppingInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ShoppingDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("shoppingdb");
            Action<NpgsqlDbContextOptionsBuilder> contextOptions = builder => builder
                .MigrationsHistoryTable("__ShoppingMigrationsHistory")
                .EnableRetryOnFailure();

            options
                .UseNpgsql(connectionString, contextOptions)
                .AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IShoppingEventStore, EfCoreShoppingEventStore>();
        services.AddScoped<IShoppingListReadModelRepository, ShoppingListReadModelRepository>();
        services.AddScoped<ShoppingListProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ShoppingItemAddedIntegrationEvent>, ShoppingListProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ShoppingItemBoughtIntegrationEvent>, ShoppingListProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ShoppingItemConsumedIntegrationEvent>, ShoppingListProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ShoppingItemRemovedIntegrationEvent>, ShoppingListProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ShoppingItemQuantityAdjustedIntegrationEvent>, ShoppingListProjectionHandler>();
        services.AddHealthChecks().AddDbContextCheck<ShoppingDbContext>("shoppingdb");

        return services;
    }
}
