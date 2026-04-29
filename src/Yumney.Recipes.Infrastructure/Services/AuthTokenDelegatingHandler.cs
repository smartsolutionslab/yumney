using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

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
