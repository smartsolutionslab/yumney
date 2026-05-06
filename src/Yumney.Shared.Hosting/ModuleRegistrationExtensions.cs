using Microsoft.AspNetCore.Builder;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Shared.Hosting;

public static class ModuleRegistrationExtensions
{
	public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder, params IModule[] modules)
	{
		foreach (var module in modules)
		{
			module.RegisterServices(builder);
		}

		return builder;
	}

	public static WebApplication RegisterEndpoints(this WebApplication app, params IModule[] modules)
	{
		foreach (var module in modules.OfType<IEndpointModule>())
		{
			module.RegisterEndpoints(app);
		}

		return app;
	}
}
