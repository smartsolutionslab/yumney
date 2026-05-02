using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

public static class RecipesApiServiceCollectionExtensions
{
	public static IServiceCollection AddRecipesApi(this IServiceCollection services)
	{
		services.AddValidatorsFromAssemblyContaining<ImportRecipeRequestValidator>();
		services.AddValidatorsFromAssemblyContaining<PhotoDataValidator>();

		return services;
	}
}
