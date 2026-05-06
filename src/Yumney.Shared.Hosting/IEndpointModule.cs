using Microsoft.AspNetCore.Builder;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Shared.Hosting;

public interface IEndpointModule : IModule
{
	WebApplication RegisterEndpoints(WebApplication app);
}
