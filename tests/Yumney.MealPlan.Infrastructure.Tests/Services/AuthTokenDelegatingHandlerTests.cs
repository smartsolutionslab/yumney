using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Services;

public class AuthTokenDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_WithBearerToken_ForwardsAuthorizationHeader()
    {
        var accessor = CreateHttpContextAccessor("Bearer test-jwt-token-123");
        using var invoker = CreateInvoker(accessor);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://recipes-api/api/v1/recipes/123");
        await invoker.SendAsync(request, CancellationToken.None);

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("test-jwt-token-123");
    }

    [Fact]
    public async Task SendAsync_WithoutAuthorizationHeader_DoesNotSetHeader()
    {
        var accessor = CreateHttpContextAccessor(null);
        using var invoker = CreateInvoker(accessor);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://recipes-api/api/v1/recipes/123");
        await invoker.SendAsync(request, CancellationToken.None);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WithEmptyAuthorizationHeader_DoesNotSetHeader()
    {
        var accessor = CreateHttpContextAccessor(string.Empty);
        using var invoker = CreateInvoker(accessor);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://recipes-api/api/v1/recipes/123");
        await invoker.SendAsync(request, CancellationToken.None);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WithNoHttpContext_DoesNotSetHeader()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        using var invoker = CreateInvoker(accessor);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://recipes-api/api/v1/recipes/123");
        await invoker.SendAsync(request, CancellationToken.None);

        request.Headers.Authorization.Should().BeNull();
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string? authorizationHeader)
    {
        var httpContext = new DefaultHttpContext();
        if (authorizationHeader is not null)
        {
            httpContext.Request.Headers.Authorization = authorizationHeader;
        }

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return accessor;
    }

    private static HttpMessageInvoker CreateInvoker(IHttpContextAccessor accessor)
    {
        var handler = new AuthTokenDelegatingHandler(accessor)
        {
            InnerHandler = new StubHandler(),
        };
        return new HttpMessageInvoker(handler);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
