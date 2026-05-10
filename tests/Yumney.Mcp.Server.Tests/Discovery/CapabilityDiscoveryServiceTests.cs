using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Discovery;

public class CapabilityDiscoveryServiceTests
{
	[Fact]
	public async Task ExecuteAsync_FetchesManifestForEveryKnownHost()
	{
		var registry = new AggregatedCapabilityRegistry();
		var factory = BuildFactoryReturningManifestForEveryHost();

		var service = new CapabilityDiscoveryService(factory, registry, NullLogger<CapabilityDiscoveryService>.Instance);
		await service.StartAsync(CancellationToken.None);
		await service.ExecuteTask!;

		registry.Manifests.Keys.Should().BeEquivalentTo(KnownCapabilityHosts.ServiceNames);
	}

	[Fact]
	public async Task ExecuteAsync_OneHostFails_OthersStillRecorded()
	{
		var registry = new AggregatedCapabilityRegistry();
		var factory = Substitute.For<IHttpClientFactory>();
		foreach (var serviceName in KnownCapabilityHosts.ServiceNames)
		{
			factory.CreateClient(serviceName).Returns(serviceName == "shopping-api"
				? FailingClient()
				: SuccessClient(new CapabilityManifest(serviceName, [Descriptor("tool_a", serviceName)])));
		}

		var service = new CapabilityDiscoveryService(factory, registry, NullLogger<CapabilityDiscoveryService>.Instance);
		await service.StartAsync(CancellationToken.None);
		await service.ExecuteTask!;

		registry.Manifests.Should().ContainKeys("recipes-api", "mealplan-api");
		registry.Manifests.Should().NotContainKey("shopping-api");
	}

	[Fact]
	public async Task ExecuteAsync_NullManifestPayload_DoesNotRecordEntry()
	{
		var registry = new AggregatedCapabilityRegistry();
		var factory = Substitute.For<IHttpClientFactory>();
		foreach (var serviceName in KnownCapabilityHosts.ServiceNames)
		{
			factory.CreateClient(serviceName).Returns(NullPayloadClient());
		}

		var service = new CapabilityDiscoveryService(factory, registry, NullLogger<CapabilityDiscoveryService>.Instance);
		await service.StartAsync(CancellationToken.None);
		await service.ExecuteTask!;

		registry.Manifests.Should().BeEmpty();
	}

	private static IHttpClientFactory BuildFactoryReturningManifestForEveryHost()
	{
		var factory = Substitute.For<IHttpClientFactory>();
		foreach (var serviceName in KnownCapabilityHosts.ServiceNames)
		{
			var manifest = new CapabilityManifest(serviceName, [Descriptor("tool_a", serviceName), Descriptor("tool_b", serviceName)]);
			factory.CreateClient(serviceName).Returns(SuccessClient(manifest));
		}

		return factory;
	}

	private static HttpClient SuccessClient(CapabilityManifest manifest) =>
		new(new StubHandler(HttpStatusCode.OK, JsonSerializer.Serialize(manifest)))
		{
			BaseAddress = new Uri("http://stub"),
		};

	private static HttpClient FailingClient() =>
		new(new StubHandler(HttpStatusCode.InternalServerError, "boom"))
		{
			BaseAddress = new Uri("http://stub"),
		};

	private static HttpClient NullPayloadClient() =>
		new(new StubHandler(HttpStatusCode.OK, "null"))
		{
			BaseAddress = new Uri("http://stub"),
		};

	private static CapabilityDescriptor Descriptor(string name, string serviceName) =>
		new(name, $"{name} on {serviceName}", CapabilitySurface.All, "GET", $"/{name}");

	private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(status)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json"),
			});
	}
}
