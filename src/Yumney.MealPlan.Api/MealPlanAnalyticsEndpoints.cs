using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

namespace SmartSolutionsLab.Yumney.MealPlan.Api;

public static class MealPlanAnalyticsEndpoints
{
	public static IEndpointRouteBuilder MapMealPlanAnalyticsEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/meal-plans");

		group.MapGet("/analytics", GetAnalytics)
			.WithName("GetMealAnalytics")
			.WithTags("MealPlan")
			.WithCapability(
				name: "get_meal_analytics",
				description: "Cooking analytics for a calendar period — pass year only for the whole year, or year + month (1-12) for a single month. Returns totals, top recipes, frequency, discovery rate, and category distribution. Use for 'how many recipes did I cook in 2025?' / 'top recipes last month' / 'was hab ich im März gekocht?'.",
				surfaces: CapabilitySurface.Mcp)
			.Produces<MealAnalyticsDto>();

		static async Task<IResult> GetAnalytics(
			IQueryHandler<GetMealAnalyticsQuery, Result<MealAnalyticsDto>> handler,
			CancellationToken cancellationToken,
			int? year = null,
			int? month = null)
		{
			var resolvedYear = year ?? DateTime.UtcNow.Year;
			var query = new GetMealAnalyticsQuery(resolvedYear, month);
			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		return app;
	}
}
