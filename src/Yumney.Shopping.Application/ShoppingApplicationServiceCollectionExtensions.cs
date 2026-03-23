using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;

namespace SmartSolutionsLab.Yumney.Shopping.Application;

public static class ShoppingApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddShoppingApplication(this IServiceCollection services)
    {
        services.AddHandlersFromAssemblyContaining<CreateShoppingListCommandHandler>();

        return services;
    }
}
