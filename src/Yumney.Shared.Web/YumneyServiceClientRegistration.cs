using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class YumneyServiceClientRegistration
{
	public static IServiceCollection AddYumneyServiceClient(this IServiceCollection services, string serviceName)
	{
		services.AddHttpContextAccessor();
		services.TryAddTransient<AuthTokenDelegatingHandler>();

		services.AddHttpClient(serviceName, client => client.BaseAddress = new Uri($"http://{serviceName}"))
			.AddHttpMessageHandler(sp => sp.GetRequiredService<AuthTokenDelegatingHandler>());

		services.TryAddSingleton<IModuleHttpClientFactory, ModuleHttpClientFactory>();

		return services;
	}
}
