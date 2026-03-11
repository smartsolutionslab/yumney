using System.Net;
using Microsoft.AspNetCore.Mvc;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Api.Middleware;

public sealed class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (GuardException ex)
        {
            logger.LogWarning(ex, "Validation failed for {Parameter}", ex.ParameterName);
            await WriteProblemDetailsAsync(context, HttpStatusCode.BadRequest, "Validation Error", ex.Message);
        }
        catch (BusinessRuleValidationException ex)
        {
            logger.LogWarning(ex, "Business rule violated: {Rule}", ex.BrokenRule.GetType().Name);
            await WriteProblemDetailsAsync(
                context,
                HttpStatusCode.UnprocessableEntity,
                "Business Rule Violation",
                ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
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
}
