using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class TestRule(string message) : IBusinessRule
    {
        public string Message => message;

        public bool IsBroken() => true;
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var wasCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_GuardException_Returns400()
    {
        var middleware = CreateMiddleware(_ => throw new GuardException("param", "Invalid value"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_GuardException_ReturnsProblemDetails()
    {
        var middleware = CreateMiddleware(_ => throw new GuardException("param", "Invalid value"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions);

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Validation Error");
        problemDetails.Detail.Should().Be("Invalid value");
    }

    [Fact]
    public async Task InvokeAsync_BusinessRuleValidationException_Returns422()
    {
        var rule = new TestRule("Business rule violated");
        var middleware = CreateMiddleware(_ => throw new BusinessRuleValidationException(rule));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Something broke"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_DoesNotLeakExceptionDetails()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Secret internal error"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        body.Should().NotContain("Secret internal error");
    }

    private static GlobalExceptionHandlerMiddleware CreateMiddleware(RequestDelegate next)
    {
        var logger = Substitute.For<ILogger<GlobalExceptionHandlerMiddleware>>();
        return new GlobalExceptionHandlerMiddleware(next, logger);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
