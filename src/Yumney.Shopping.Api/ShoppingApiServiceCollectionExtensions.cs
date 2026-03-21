using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shopping.Api.Requests;

namespace SmartSolutionsLab.Yumney.Shopping.Api;

public static class ShoppingApiServiceCollectionExtensions
{
    public static IServiceCollection AddShoppingApi(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateShoppingListRequestValidator>();

        return services;
    }
}
