using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Users.Application;

public sealed class UsersApplicationModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.Services.AddUsersApplication();
		return builder;
	}
}
