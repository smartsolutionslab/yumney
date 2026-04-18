using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Services;

public sealed class AuthTokenDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
