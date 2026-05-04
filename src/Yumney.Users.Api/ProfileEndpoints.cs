using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
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

		group.MapGet("/", GetProfile)
			.WithName("GetUserProfile")
			.WithTags("Users")
			.Produces<UserProfileDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> GetProfile(
			ICommandHandler<EnsureUserProfileCommand, Result> ensure,
			IQueryHandler<GetUserProfileQuery, Result<UserProfileDto>> handler,
			CancellationToken cancellationToken)
		{
			// Idempotent JIT-provisioning for Keycloak-authenticated users who
			// have no AppUserProfile row yet (realm-seeded users, SSO, admin).
			// Any failure cascades naturally to the query's 404.
			await ensure.HandleAsync(new EnsureUserProfileCommand(), cancellationToken);

			var result = await handler.HandleAsync(new GetUserProfileQuery(), cancellationToken);
			return result.ToOk();
		}

		group.MapPut("/", UpdateProfile)
			.WithName("UpdateUserProfile")
			.WithTags("Users")
			.Produces<UserProfileDto>()
			.ProducesValidationProblem();

		static async Task<IResult> UpdateProfile(
			UpdateUserProfileCommand command,
			ICommandHandler<UpdateUserProfileCommand, Result<UserProfileDto>> handler,
			CancellationToken cancellationToken)
		{
			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		return app;
	}
}
