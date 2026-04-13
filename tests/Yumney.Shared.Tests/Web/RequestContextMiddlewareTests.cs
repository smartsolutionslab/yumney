using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class RequestContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_DelegatesDownstream()
    {
        var wasCalled = false;
        var logger = Substitute.For<ILogger<RequestContextMiddleware>>();
        RequestDelegate next = _ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new RequestContextMiddleware(next, logger);

        var context = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-42") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        context.Items[CorrelationIdMiddleware.HeaderName] = "corr-abc";

        await middleware.InvokeAsync(context);

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_AnonymousUser_DelegatesDownstream()
    {
        var wasCalled = false;
        var logger = Substitute.For<ILogger<RequestContextMiddleware>>();
        RequestDelegate next = _ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new RequestContextMiddleware(next, logger);

        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        wasCalled.Should().BeTrue();
    }
}
