using SmartSolutionsLab.Yumney.MealPlan.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore.Events;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

namespace SmartSolutionsLab.Yumney.MealPlan.Api;

public sealed class MealPlanApiModule : IEndpointModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		((WebApplicationBuilder)builder).AddYumneyDefaults(
			outboxConnectionName: "mealplandb",
			outboxSchema: "wolverine_mealplan",
			typeof(ExtendedModeDisabledModuleEvent).Assembly,
			typeof(UserAccountDeletedHandler).Assembly);

		builder.Services.AddCqrsLoggingDecorators();

		return builder;
	}

	public WebApplication RegisterEndpoints(WebApplication app)
	{
		app
			.UseYumneyDefaults()
			.MapApiV1()
			.MapMealPlanEndpoints();
		app.MapCapabilityManifest("mealplan-api");
		return app;
	}
}
