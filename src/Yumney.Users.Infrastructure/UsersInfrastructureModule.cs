using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure;

public sealed class UsersInfrastructureModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.Services.AddUsersInfrastructure(builder.Configuration);
		return builder;
	}
}
