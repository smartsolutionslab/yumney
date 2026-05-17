using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Web;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class ModuleHttpClientFactoryTests
{
	[Fact]
	public void For_ResolvesNamedHttpClientAndWrapsItInModuleHttpClient()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddHttpClient("recipes-api", client => client.BaseAddress = new Uri("https://example.com/"));
		using var provider = services.BuildServiceProvider();

		var factory = new ModuleHttpClientFactory(provider);
		var client = factory.For("recipes-api");

		client.Should().NotBeNull();
	}

	[Fact]
	public void For_DifferentUpstreamNames_ReturnsDistinctClients()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddHttpClient("a");
		services.AddHttpClient("b");
		using var provider = services.BuildServiceProvider();

		var factory = new ModuleHttpClientFactory(provider);
		var first = factory.For("a");
		var second = factory.For("b");

		first.Should().NotBeSameAs(second);
	}
}
