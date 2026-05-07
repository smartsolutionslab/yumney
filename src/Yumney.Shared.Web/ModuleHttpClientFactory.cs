using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Yumney.Shared.Web;

internal sealed class ModuleHttpClientFactory(IServiceProvider serviceProvider) : IModuleHttpClientFactory
{
	public IModuleHttpClient For(string upstreamName)
	{
		var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
		var logger = serviceProvider.GetRequiredService<ILogger<ModuleHttpClient>>();
		var client = httpClientFactory.CreateClient(upstreamName);
		return new ModuleHttpClient(client, upstreamName, logger);
	}
}
