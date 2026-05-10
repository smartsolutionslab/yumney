using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using NSubstitute;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Mcp.Server.Mcp;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Mcp;

public class RestProxyServiceTests
{
	[Fact]
	public async Task InvokeAsync_UnknownTool_ReturnsErrorResult()
	{
		var registry = new AggregatedCapabilityRegistry();
		var service = BuildService(registry, BuildClientFactory(_ => SuccessClient("ignored")), bearer: "abc");

		var result = await service.InvokeAsync("nonexistent_tool", arguments: null, CancellationToken.None);

		result.IsError.Should().BeTrue();
		result.Content.OfType<TextContentBlock>().Single().Text.Should().Contain("Unknown tool");
	}

	[Fact]
	public async Task InvokeAsync_NoBearer_ReturnsErrorResult()
	{
		var registry = BuildRegistryWith(Descriptor("search_recipes", "GET", "/api/v1/recipes/"));
		var service = BuildService(registry, BuildClientFactory(_ => SuccessClient("ignored")), bearer: null);

		var result = await service.InvokeAsync("search_recipes", arguments: null, CancellationToken.None);

		result.IsError.Should().BeTrue();
		result.Content.OfType<TextContentBlock>().Single().Text.Should().Contain("No bearer token");
	}

	[Fact]
	public async Task InvokeAsync_MissingPlaceholder_ReturnsErrorResult()
	{
		var registry = BuildRegistryWith(Descriptor("get_recipe", "GET", "/api/v1/recipes/{identifier:guid}"));
		var service = BuildService(registry, BuildClientFactory(_ => SuccessClient("ignored")), bearer: "abc");

		var result = await service.InvokeAsync("get_recipe", arguments: null, CancellationToken.None);

		result.IsError.Should().BeTrue();
		result.Content.OfType<TextContentBlock>().Single().Text.Should().Contain("identifier");
	}

	[Fact]
	public async Task InvokeAsync_HappyPath_ForwardsBearerAndReturnsResponseBody()
	{
		var registry = BuildRegistryWith(Descriptor("search_recipes", "GET", "/api/v1/recipes/"));
		HttpRequestMessage? capturedRequest = null;
		var service = BuildService(
			registry,
			BuildClientFactory(captured =>
			{
				capturedRequest = captured;
				return SuccessClient("[{\"id\":\"abc\"}]");
			}),
			bearer: "the-bearer");

		var result = await service.InvokeAsync("search_recipes", arguments: null, CancellationToken.None);

		result.IsError.Should().BeFalse();
		result.Content.OfType<TextContentBlock>().Single().Text.Should().Contain("\"id\":\"abc\"");
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
		capturedRequest.Headers.Authorization.Parameter.Should().Be("the-bearer");
		capturedRequest.Method.Method.Should().Be("GET");
	}

	[Fact]
	public async Task InvokeAsync_UpstreamReturnsNonSuccess_ReturnsErrorResult()
	{
		var registry = BuildRegistryWith(Descriptor("search_recipes", "GET", "/api/v1/recipes/"));
		var service = BuildService(
			registry,
			BuildClientFactory(_ => FailingClient(HttpStatusCode.InternalServerError, "boom")),
			bearer: "abc");

		var result = await service.InvokeAsync("search_recipes", arguments: null, CancellationToken.None);

		result.IsError.Should().BeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("500");
		text.Should().Contain("boom");
	}

	[Fact]
	public async Task InvokeAsync_PostBodyForwardedAsJson()
	{
		var registry = BuildRegistryWith(Descriptor(
			"create_shopping_list_from_recipes",
			"POST",
			"/api/v1/shopping-lists/from-recipes"));
		string? capturedMethod = null;
		string? capturedBody = null;
		var factory = Substitute.For<IHttpClientFactory>();
		factory.CreateClient(Arg.Any<string>()).Returns(_ =>
			new HttpClient(new BodyCapturingHandler(async (request, ct) =>
			{
				capturedMethod = request.Method.Method;
				capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("{\"id\":\"new-list\"}", Encoding.UTF8, "application/json"),
				};
			}))
			{
				BaseAddress = new Uri("http://stub"),
			});
		var service = BuildService(registry, factory, bearer: "abc");

		var arguments = new Dictionary<string, JsonElement>
		{
			["title"] = JsonSerializer.SerializeToElement("This week"),
		};
		var result = await service.InvokeAsync("create_shopping_list_from_recipes", arguments, CancellationToken.None);

		result.IsError.Should().BeFalse();
		capturedMethod.Should().Be("POST");
		capturedBody.Should().NotBeNull().And.Subject.Should().Contain("\"title\"");
	}

	private static AggregatedCapabilityRegistry BuildRegistryWith(CapabilityDescriptor descriptor)
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest("recipes-api", [descriptor]));
		return registry;
	}

	private static CapabilityDescriptor Descriptor(string name, string method, string route) =>
		new(name, $"description for {name}", CapabilitySurface.Mcp, method, route);

	private static RestProxyService BuildService(
		AggregatedCapabilityRegistry registry,
		IHttpClientFactory factory,
		string? bearer)
	{
		var contextAccessor = Substitute.For<IHttpContextAccessor>();
		if (bearer is not null)
		{
			var context = new DefaultHttpContext();
			context.Request.Headers.Authorization = $"Bearer {bearer}";
			contextAccessor.HttpContext.Returns(context);
		}

		return new RestProxyService(registry, factory, contextAccessor, NullLogger<RestProxyService>.Instance);
	}

	private static IHttpClientFactory BuildClientFactory(Func<HttpRequestMessage, HttpClient> handler)
	{
		var factory = Substitute.For<IHttpClientFactory>();
		factory.CreateClient(Arg.Any<string>())
			.Returns(callInfo => new HttpClient(new CapturingHandler(captured => handler(captured)))
			{
				BaseAddress = new Uri("http://stub"),
			});
		return factory;
	}

	private static HttpClient SuccessClient(string body) =>
		new(new StubHandler(HttpStatusCode.OK, body))
		{
			BaseAddress = new Uri("http://stub"),
		};

	private static HttpClient FailingClient(HttpStatusCode status, string body) =>
		new(new StubHandler(status, body))
		{
			BaseAddress = new Uri("http://stub"),
		};

	private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(status)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json"),
			});
	}

	private sealed class CapturingHandler(Func<HttpRequestMessage, HttpClient> upstream) : HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			using var client = upstream(request);
			using var inner = await client.SendAsync(new HttpRequestMessage(request.Method, "/"), cancellationToken);
			var body = await inner.Content.ReadAsStringAsync(cancellationToken);
			return new HttpResponseMessage(inner.StatusCode)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json"),
			};
		}
	}

	private sealed class BodyCapturingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			respond(request, cancellationToken);
	}
}
