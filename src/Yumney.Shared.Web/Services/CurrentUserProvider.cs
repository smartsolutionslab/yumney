using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shared.Web.Services;

public sealed class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User?.FindFirst("sub")?.Value
                            ?? string.Empty;

    public string Email => User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value
        ?? string.Empty;

    public string DisplayName => User?.FindFirst("preferred_username")?.Value
        ?? User?.FindFirst(ClaimTypes.Name)?.Value
        ?? string.Empty;

    public IReadOnlyCollection<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList()
        .AsReadOnly()
        ?? (IReadOnlyCollection<string>)Array.Empty<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
}
