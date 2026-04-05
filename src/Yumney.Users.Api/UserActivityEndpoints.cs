using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Application.Queries;

namespace SmartSolutionsLab.Yumney.Users.Api;

public static class UserActivityEndpoints
{
    public static IEndpointRouteBuilder MapUserActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users/me");

        group.MapGet("/activity", GetRecentActivityAsync)
            .RequireAuthorization()
            .WithName("GetRecentActivity")
            .WithTags("Users")
            .Produces<IReadOnlyList<UserActivityDto>>();

        group.MapGet("/suggestions", GetSuggestionsAsync)
            .RequireAuthorization()
            .WithName("GetSuggestions")
            .WithTags("Users")
            .Produces<SuggestionsResponseDto>();

        return app;
    }

    private static async Task<IResult> GetRecentActivityAsync(
        IQueryHandler<GetRecentActivityQuery, Result<IReadOnlyList<UserActivityDto>>> handler,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRecentActivityQuery(limit);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> GetSuggestionsAsync(
        IQueryHandler<GetSuggestionsQuery, Result<SuggestionsResponseDto>> handler,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSuggestionsQuery();
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
    }
}
