using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Application.Commands;

namespace SmartSolutionsLab.Yumney.Users.Api;

/// <summary>
/// Irreversible operations on the current user's account (US-101 / GDPR Art. 17).
/// Kept in its own endpoint group so the destructive surface is easy to audit.
/// </summary>
public static class DangerZoneEndpoints
{
	public static IEndpointRouteBuilder MapDangerZoneEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/users/me");

		group.MapDelete("/", DeleteAccount)
			.WithName("DeleteAccount")
			.WithTags("Users")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesProblem(StatusCodes.Status503ServiceUnavailable);

		static async Task<IResult> DeleteAccount(ICommandHandler<DeleteAccountCommand, Result> handler, CancellationToken cancellationToken)
		{
			var result = await handler.HandleAsync(new DeleteAccountCommand(), cancellationToken);
			return result.ToNoContent();
		}

		return app;
	}
}
