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
		var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
		if (authHeader.HasValue())
		{
			request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader!);
		}

		return base.SendAsync(request, cancellationToken);
	}
}
