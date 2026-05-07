using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Application.Queries;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Api;

public static class UserActivityEndpoints
{
	public static IEndpointRouteBuilder MapUserActivityEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/users/me");

		group.MapGet("/activity", GetRecentActivity)
			.RequireAuthorization()
			.WithName("GetRecentActivity")
			.WithTags("Users")
			.Produces<UserActivityPageDto>();

		static async Task<IResult> GetRecentActivity(
			IQueryHandler<GetRecentActivityQuery, Result<UserActivityPageDto>> handler,
			int limit = 5,
			string? type = null,
			string? cursor = null,
			CancellationToken cancellationToken = default)
		{
			var typeFilter = string.IsNullOrWhiteSpace(type) ? null : ActivityType.From(type);
			var decodedCursor = ActivityCursor.TryDecode(cursor);
			var query = new GetRecentActivityQuery(ActivityLimit.From(limit), typeFilter, decodedCursor);
			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/activity/recipes/{identifier:guid}/stats", GetRecipeStats)
			.RequireAuthorization()
			.WithName("GetRecipeActivityStats")
			.WithTags("Users")
			.Produces<RecipeActivityStatsDto>();

		static async Task<IResult> GetRecipeStats(
			Guid identifier,
			IQueryHandler<GetRecipeActivityStatsQuery, Result<RecipeActivityStatsDto>> handler,
			CancellationToken cancellationToken)
		{
			var result = await handler.HandleAsync(new GetRecipeActivityStatsQuery(identifier), cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/suggestions", GetSuggestions)
			.RequireAuthorization()
			.WithName("GetSuggestions")
			.WithTags("Users")
			.Produces<SuggestionsResponseDto>();

		static async Task<IResult> GetSuggestions(
			IQueryHandler<GetSuggestionsQuery, Result<SuggestionsResponseDto>> handler,
			CancellationToken cancellationToken = default)
		{
			var query = new GetSuggestionsQuery();
			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		return app;
	}
}
