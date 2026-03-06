using Microsoft.AspNetCore.Routing;

namespace Yumney.Users.Api;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();

        return app;
    }
}
