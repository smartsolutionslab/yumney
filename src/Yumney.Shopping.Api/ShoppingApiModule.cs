using FluentValidation;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;
using SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure;

namespace SmartSolutionsLab.Yumney.Shopping.Api;

public sealed class ShoppingApiModule : IEndpointModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		((WebApplicationBuilder)builder).AddYumneyDefaults(
			typeof(ShoppingInfrastructureServiceCollectionExtensions).Assembly,
			typeof(RecipeDeletedHandler).Assembly);

		builder.Services.AddValidatorsFromAssemblyContaining<CreateShoppingListValidator>();

		builder.Services.AddCqrsLoggingDecorators();

		return builder;
	}

	public WebApplication RegisterEndpoints(WebApplication app)
	{
		app.UseYumneyDefaults().MapApiV1().MapShoppingEndpoints();
		return app;
	}
}
