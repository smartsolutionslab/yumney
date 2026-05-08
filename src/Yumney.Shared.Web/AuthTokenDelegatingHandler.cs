using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shared.Web;

/// <summary>
/// Forwards the inbound caller's bearer token onto outbound HTTP requests so
/// service-to-service calls reach downstream APIs as the original user.
/// Register on a typed/named <c>HttpClient</c> via
/// <c>AddHttpMessageHandler&lt;AuthTokenDelegatingHandler&gt;()</c>.
/// </summary>
public sealed class AuthTokenDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (ShouldForwardAuth(request))
		{
			var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
			if (authHeader.HasValue())
			{
				request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader!);
			}
		}

		return base.SendAsync(request, cancellationToken);
	}

	// Only forward the bearer to internal Yumney services. Aspire service-discovery
	// hostnames are bare labels ("users-api", "shopping-api") with no dots; anything
	// dotted is publicly resolvable and never an internal target — refuse to leak
	// the user's token to it even if a typed client is misconfigured.
	private static bool ShouldForwardAuth(HttpRequestMessage request)
	{
		var host = request.RequestUri?.Host;
		return !string.IsNullOrEmpty(host) && !host.Contains('.');
	}
}
