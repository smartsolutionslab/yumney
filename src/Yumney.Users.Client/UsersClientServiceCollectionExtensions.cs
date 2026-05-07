using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Users.Client;

public static class UsersClientServiceCollectionExtensions
{
	public static IServiceCollection AddUsersClient(this IServiceCollection services)
	{
		services.AddYumneyServiceClient("users-api");
		services.AddSingleton<IUsersClient, UsersClient>();
		return services;
	}
}
