using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

namespace SmartSolutionsLab.Yumney.Users.Api;

public static class StaplesEndpoints
{
	public static IEndpointRouteBuilder MapStaplesEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/users/staples");

		group.MapGet("/", GetStaples)
			.WithName("GetUserStaples")
			.WithTags("Users")
			.Produces<IReadOnlyList<string>>();

		return app;

		static async Task<IResult> GetStaples(IStaplesProvider staplesProvider, ICurrentUser currentUser, CancellationToken cancellationToken)
		{
			var owner = OwnerIdentifier.From(currentUser.UserId);
			var staples = await staplesProvider.GetStapleNamesAsync(owner, cancellationToken);
			return Results.Ok(staples);
		}
	}
}
