using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure;

public static class ShoppingInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddShoppingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ShoppingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("shoppingdb"),
                x => x.MigrationsHistoryTable("__ShoppingMigrationsHistory")));

        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();

        return services;
    }
}
