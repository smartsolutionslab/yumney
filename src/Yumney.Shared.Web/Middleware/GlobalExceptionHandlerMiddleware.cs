using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Web.Middleware;

#pragma warning disable SA1601
public sealed partial class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (GuardException ex)
        {
            LogValidationFailed(ex, ex.ParameterName);
            await WriteProblemDetailsAsync(context, HttpStatusCode.BadRequest, "Validation Error", ex.Message);
        }
        catch (BusinessRuleValidationException ex)
        {
            LogBusinessRuleViolated(ex, ex.BrokenRule.GetType().Name);
            await WriteProblemDetailsAsync(
                context,
                HttpStatusCode.UnprocessableEntity,
                "Business Rule Violation",
                ex.Message);
        }
        catch (Exception ex)
        {
            LogUnhandledException(ex);
            await WriteProblemDetailsAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation failed for {Parameter}")]
    private partial void LogValidationFailed(Exception ex, string parameter);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Business rule violated: {Rule}")]
    private partial void LogBusinessRuleViolated(Exception ex, string rule);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception occurred")]
    private partial void LogUnhandledException(Exception ex);
}
