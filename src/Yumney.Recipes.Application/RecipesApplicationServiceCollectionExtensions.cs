using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

namespace SmartSolutionsLab.Yumney.Recipes.Application;

public static class RecipesApplicationServiceCollectionExtensions
{
	public static IServiceCollection AddRecipesApplication(this IServiceCollection services)
	{
		services.AddHandlersFromAssemblyContaining<ImportRecipeCommandHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingListCreatedCrossModuleIntegrationEvent>, ShoppingListCreatedHandler>();
		services.AddScoped<IIntegrationEventHandler<ShoppingListRecipeReferenceClearedCrossModuleIntegrationEvent>, ShoppingListRecipeReferenceClearedHandler>();

		return services;
	}
}
