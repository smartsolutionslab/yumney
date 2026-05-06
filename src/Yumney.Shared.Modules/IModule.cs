using Microsoft.Extensions.Hosting;

namespace SmartSolutionsLab.Yumney.Shared.Modules;

public interface IModule
{
	IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder);
}
