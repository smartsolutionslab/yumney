using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

public sealed class RecipesApiModule : IEndpointModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		((WebApplicationBuilder)builder).AddYumneyDefaults(typeof(ShoppingListCreatedHandler).Assembly);

		builder.Services.AddValidatorsFromAssemblyContaining<ImportRecipeValidator>();
		builder.Services.AddValidatorsFromAssemblyContaining<PhotoDataValidator>();

		builder.Services.AddCqrsLoggingDecorators();

		return builder;
	}

	public WebApplication RegisterEndpoints(WebApplication app)
	{
		app
			.UseYumneyDefaults()
			.MapApiV1()
			.MapRecipesEndpoints();
		return app;
	}
}
