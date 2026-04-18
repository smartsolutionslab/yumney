using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application;

public static class RecipesApplicationServiceCollectionExtensions
{
	public static IServiceCollection AddRecipesApplication(this IServiceCollection services)
	{
		services.AddHandlersFromAssemblyContaining<ImportRecipeCommandHandler>();

		return services;
	}
}
