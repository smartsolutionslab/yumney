using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

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
        services.AddHealthChecks().AddDbContextCheck<ShoppingDbContext>("shoppingdb");

        return services;
    }
}
