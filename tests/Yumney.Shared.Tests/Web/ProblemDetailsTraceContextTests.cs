using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class ProblemDetailsTraceContextTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
	};

	[Fact]
	public async Task InvokeAsync_Exception_IncludesCorrelationIdInProblemDetails()
	{
		var logger = Substitute.For<ILogger<GlobalExceptionHandlerMiddleware>>();
		var middleware = new GlobalExceptionHandlerMiddleware(
			_ => throw new InvalidOperationException("fail"), logger);

		var context = new DefaultHttpContext();
		context.Response.Body = new MemoryStream();
		context.Items[CorrelationIdMiddleware.HeaderName] = "test-corr-123";

		await middleware.InvokeAsync(context);

		context.Response.Body.Seek(0, SeekOrigin.Begin);
		var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
		var doc = JsonDocument.Parse(body);

		doc.RootElement.TryGetProperty("correlationId", out var corrId).Should().BeTrue();
		corrId.GetString().Should().Be("test-corr-123");
	}

	[Fact]
	public async Task InvokeAsync_Exception_IncludesTraceIdFieldInProblemDetails()
	{
		var logger = Substitute.For<ILogger<GlobalExceptionHandlerMiddleware>>();
		var middleware = new GlobalExceptionHandlerMiddleware(
			_ => throw new InvalidOperationException("fail"), logger);

		var context = new DefaultHttpContext();
		context.Response.Body = new MemoryStream();

		await middleware.InvokeAsync(context);

		context.Response.Body.Seek(0, SeekOrigin.Begin);
		var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
		var doc = JsonDocument.Parse(body);

		// traceId field should exist (may be null if no Activity is active in test context)
		doc.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
	}
}
