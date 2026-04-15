using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

public static class RecipesApiServiceCollectionExtensions
{
    public static IServiceCollection AddRecipesApi(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ImportRecipeRequestValidator>();

        return services;
    }
}
