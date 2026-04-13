using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Yumney.Shared.Web.Middleware;

#pragma warning disable SA1601
#pragma warning disable SA1311
public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly HashSet<string> excludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/alive",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (excludedPaths.Contains(path))
        {
            await next(context);
            return;
        }

        var method = context.Request.Method;
        var start = Stopwatch.GetTimestamp();

        await next(context);

        var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var statusCode = context.Response.StatusCode;

        if (statusCode >= 500)
        {
            LogServerError(method, path, statusCode, elapsed);
        }
        else if (statusCode >= 400)
        {
            LogClientError(method, path, statusCode, elapsed);
        }
        else
        {
            LogSuccess(method, path, statusCode, elapsed);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs:F1}ms")]
    private partial void LogSuccess(string method, string path, int statusCode, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs:F1}ms")]
    private partial void LogClientError(string method, string path, int statusCode, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs:F1}ms")]
    private partial void LogServerError(string method, string path, int statusCode, double elapsedMs);
}
