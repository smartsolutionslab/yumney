using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Yumney.Shared.Web.Middleware;

public sealed class RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
{
	public async Task InvokeAsync(HttpContext context)
	{
		var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? context.User?.FindFirst(KeycloakClaimTypes.Subject)?.Value
			?? "anonymous";

		var correlationId = context.Items[CorrelationIdMiddleware.HeaderName] as string ?? "unknown";

		Dictionary<string, object?> state = new()
		{
			["UserId"] = userId,
			["CorrelationId"] = correlationId,
			["RequestMethod"] = context.Request.Method,
			["RequestPath"] = context.Request.Path.Value,
		};

		using (logger.BeginScope(state))
		{
			await next(context).ConfigureAwait(false);
		}
	}
}
