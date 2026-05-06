using Microsoft.AspNetCore.Builder;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Shared.Hosting;

public static class ModuleHost
{
	public static void Run(string[] args, params IModule[] modules)
	{
		var builder = WebApplication.CreateBuilder(args);
		foreach (var module in modules)
		{
			module.RegisterServices(builder);
		}

		var app = builder.Build();
		foreach (var module in modules.OfType<IEndpointModule>())
		{
			module.RegisterEndpoints(app);
		}

		app.Run();
	}
}
