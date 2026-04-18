using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class CorrelationIdMiddlewareTests
{
	[Fact]
	public async Task InvokeAsync_NoHeader_GeneratesCorrelationId()
	{
		string? capturedId = null;
		var middleware = new CorrelationIdMiddleware(ctx =>
		{
			capturedId = ctx.Items[CorrelationIdMiddleware.HeaderName] as string;
			return Task.CompletedTask;
		});
		var context = new DefaultHttpContext();

		await middleware.InvokeAsync(context);

		capturedId.Should().NotBeNullOrEmpty();
		capturedId.Should().HaveLength(32);
	}

	[Fact]
	public async Task InvokeAsync_WithHeader_PreservesExistingId()
	{
		string? capturedId = null;
		var middleware = new CorrelationIdMiddleware(ctx =>
		{
			capturedId = ctx.Items[CorrelationIdMiddleware.HeaderName] as string;
			return Task.CompletedTask;
		});
		var context = new DefaultHttpContext();
		context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "my-custom-id";

		await middleware.InvokeAsync(context);

		capturedId.Should().Be("my-custom-id");
	}

	[Fact]
	public async Task InvokeAsync_SetsCorrelationIdInContextItems()
	{
		var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask);
		var context = new DefaultHttpContext();
		context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "test-123";

		await middleware.InvokeAsync(context);

		context.Items[CorrelationIdMiddleware.HeaderName].Should().Be("test-123");
	}
}
