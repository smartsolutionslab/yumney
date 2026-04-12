using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Application.Queries;

namespace SmartSolutionsLab.Yumney.Users.Api;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users/me/profile");

        group.MapGet("/", GetProfileAsync)
            .WithName("GetUserProfile")
            .WithTags("Users")
            .Produces<UserProfileDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/", UpdateProfileAsync)
            .WithName("UpdateUserProfile")
            .WithTags("Users")
            .Produces<UserProfileDto>()
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> GetProfileAsync(
        IQueryHandler<GetUserProfileQuery, Result<UserProfileDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUserProfileQuery(), cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateUserProfileCommand command,
        ICommandHandler<UpdateUserProfileCommand, Result<UserProfileDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }
}
