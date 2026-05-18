using FluentValidation;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Api.Requests;
using SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;

namespace SmartSolutionsLab.Yumney.Users.Api;

public sealed class UsersApiModule : IEndpointModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		((WebApplicationBuilder)builder).AddYumneyDefaults(
			outboxConnectionName: "usersdb",
			outboxSchema: "wolverine_users",
			typeof(RecipeImportedActivityHandler).Assembly);
		builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();

		builder.Services.AddCqrsLoggingDecorators();

		return builder;
	}

	public WebApplication RegisterEndpoints(WebApplication app)
	{
		app.UseYumneyDefaults()
			.MapApiV1()
			.MapUsersEndpoints();

		return app;
	}
}
