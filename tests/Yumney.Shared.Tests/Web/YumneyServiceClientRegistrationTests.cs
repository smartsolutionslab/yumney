using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Web;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class YumneyServiceClientRegistrationTests
{
	[Fact]
	public void AddYumneyServiceClient_RegistersNamedHttpClientWithExpectedBaseAddress()
	{
		ServiceCollection services = [];

		services.AddYumneyServiceClient("recipes-api");

		using var provider = services.BuildServiceProvider();
		var factory = provider.GetRequiredService<IHttpClientFactory>();
		var client = factory.CreateClient("recipes-api");

		client.BaseAddress.Should().Be(new Uri("http://recipes-api"));
	}

	[Fact]
	public void AddYumneyServiceClient_CalledTwice_DoesNotDuplicateAuthHandler()
	{
		ServiceCollection services = [];

		services.AddYumneyServiceClient("recipes-api");
		services.AddYumneyServiceClient("shopping-api");

		var matchingDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(AuthTokenDelegatingHandler)).ToList();
		matchingDescriptors.Should().HaveCount(1, "TryAddTransient must only register the handler once even when multiple service clients are added");
	}

	[Fact]
	public void AddYumneyServiceClient_RegistersAuthTokenDelegatingHandler()
	{
		ServiceCollection services = [];

		services.AddYumneyServiceClient("recipes-api");

		using var provider = services.BuildServiceProvider();
		var handler = provider.GetService<AuthTokenDelegatingHandler>();

		handler.Should().NotBeNull("AddYumneyServiceClient must register AuthTokenDelegatingHandler so the named HttpClient can resolve it");
	}
}
