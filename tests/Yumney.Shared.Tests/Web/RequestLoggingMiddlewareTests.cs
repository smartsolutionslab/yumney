using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class RequestLoggingMiddlewareTests
{
	[Fact]
	public async Task InvokeAsync_SuccessResponse_DelegatesDownstream()
	{
		var wasCalled = false;
		var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
		RequestDelegate next = ctx =>
		{
			wasCalled = true;
			ctx.Response.StatusCode = 200;
			return Task.CompletedTask;
		};
		var middleware = new RequestLoggingMiddleware(next, logger);

		await middleware.InvokeAsync(new DefaultHttpContext());

		wasCalled.Should().BeTrue();
	}

	[Fact]
	public async Task InvokeAsync_HealthEndpoint_SkipsLogging()
	{
		var wasCalled = false;
		var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
		RequestDelegate next = _ =>
		{
			wasCalled = true;
			return Task.CompletedTask;
		};
		var middleware = new RequestLoggingMiddleware(next, logger);

		var context = new DefaultHttpContext();
		context.Request.Path = "/health";

		await middleware.InvokeAsync(context);

		wasCalled.Should().BeTrue();
	}

	[Fact]
	public async Task InvokeAsync_AliveEndpoint_SkipsLogging()
	{
		var wasCalled = false;
		var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
		RequestDelegate next = _ =>
		{
			wasCalled = true;
			return Task.CompletedTask;
		};
		var middleware = new RequestLoggingMiddleware(next, logger);

		var context = new DefaultHttpContext();
		context.Request.Path = "/alive";

		await middleware.InvokeAsync(context);

		wasCalled.Should().BeTrue();
	}

	[Fact]
	public async Task InvokeAsync_404Response_DelegatesDownstream()
	{
		var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
		RequestDelegate next = ctx =>
		{
			ctx.Response.StatusCode = 404;
			return Task.CompletedTask;
		};
		var middleware = new RequestLoggingMiddleware(next, logger);

		var context = new DefaultHttpContext();
		context.Request.Method = "GET";
		context.Request.Path = "/api/v1/recipes/123";

		await middleware.InvokeAsync(context);

		context.Response.StatusCode.Should().Be(404);
	}

	[Fact]
	public async Task InvokeAsync_500Response_DelegatesDownstream()
	{
		var logger = Substitute.For<ILogger<RequestLoggingMiddleware>>();
		RequestDelegate next = ctx =>
		{
			ctx.Response.StatusCode = 500;
			return Task.CompletedTask;
		};
		var middleware = new RequestLoggingMiddleware(next, logger);

		var context = new DefaultHttpContext();
		context.Request.Method = "POST";
		context.Request.Path = "/api/v1/recipes";

		await middleware.InvokeAsync(context);

		context.Response.StatusCode.Should().Be(500);
	}
}
