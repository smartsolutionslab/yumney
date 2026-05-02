using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Recipes.Application;

public static class RecipesApplicationServiceCollectionExtensions
{
	public static IServiceCollection AddRecipesApplication(this IServiceCollection services)
	{
		services.AddHandlersFromAssemblyContaining<ImportRecipeCommandHandler>();
		services.AddIntegrationEventHandlersFromAssemblyContaining<ImportRecipeCommandHandler>();

		return services;
	}
}
