using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

namespace SmartSolutionsLab.Yumney.MealPlan.Api;

public static class MealPlanSuggestionEndpoints
{
	public static IEndpointRouteBuilder MapMealPlanSuggestionEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/meal-plans");

		group.MapPost("/{year:int}/w/{weekNumber:int}/suggest", SuggestWeekPlan)
			.WithName("SuggestWeekPlan")
			.WithTags("MealPlan")
			.WithCapability(
				name: "suggest_week_plan",
				description: "Suggest a complete dinner plan for an ISO week ('plan my week', 'fill out next week') using the user's recipe catalog, history, and dietary profile.",
				surfaces: CapabilitySurface.Chat | CapabilitySurface.Mcp)
			.Produces<WeekSuggestionDto>()
			.ProducesProblem(StatusCodes.Status422UnprocessableEntity);

		static async Task<IResult> SuggestWeekPlan(
			int year,
			int weekNumber,
			IQueryHandler<SuggestWeekPlanQuery, Result<WeekSuggestionDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var result = await handler.HandleAsync(new SuggestWeekPlanQuery(week), cancellationToken);
			return result.ToOk();
		}

		return app;
	}
}
