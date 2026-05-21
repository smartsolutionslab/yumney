using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

namespace SmartSolutionsLab.Yumney.MealPlan.Api;

#pragma warning disable SA1601
public static partial class MealPlanEndpoints
#pragma warning restore SA1601
{
	public static IEndpointRouteBuilder MapMealPlanEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/meal-plans");

		group.MapGet("/{year:int}/w/{weekNumber:int}", GetWeeklyPlan)
			.WithName("GetWeeklyPlan")
			.WithTags("MealPlan")
			.WithCapability(
				name: "get_weekly_plan",
				description: "Fetch the user's planned meals for an ISO week. Use for 'what's for dinner this week?' / 'show me next week's plan'.",
				surfaces: CapabilitySurface.All)
			.Produces<WeeklyPlanDto>();

		static async Task<IResult> GetWeeklyPlan(
			int year,
			int weekNumber,
			IQueryHandler<GetWeeklyPlanQuery, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var query = new GetWeeklyPlanQuery(WeekIdentifier.From(year, weekNumber));
			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/{year:int}/w/{weekNumber:int}/planned-recipes", GetPlannedRecipes)
			.WithName("GetPlannedRecipes")
			.WithTags("MealPlan")
			.Produces<WeeklyPlannedRecipesDto>();

		static async Task<IResult> GetPlannedRecipes(
			int year,
			int weekNumber,
			IQueryHandler<GetPlannedRecipesQuery, Result<WeeklyPlannedRecipesDto>> handler,
			CancellationToken cancellationToken)
		{
			var result = await handler.HandleAsync(new GetPlannedRecipesQuery(WeekIdentifier.From(year, weekNumber)), cancellationToken);
			return result.ToOk();
		}

		group.MapPut("/{year:int}/w/{weekNumber:int}/extended-mode", ToggleExtendedMode)
			.WithName("ToggleExtendedMode")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>();

		static async Task<IResult> ToggleExtendedMode(
			int year,
			int weekNumber,
			Requests.ToggleExtendedMode request,
			ICommandHandler<ToggleExtendedModeCommand, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var command = new ToggleExtendedModeCommand(WeekIdentifier.From(year, weekNumber), request.Enable);
			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/{year:int}/w/{weekNumber:int}/generate-shopping-list", GenerateShoppingList)
			.WithName("GenerateShoppingList")
			.WithTags("MealPlan")
			.Produces<GenerateShoppingListResultDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest);

		static async Task<IResult> GenerateShoppingList(
			int year,
			int weekNumber,
			ICommandHandler<GenerateShoppingListCommand, Result<GenerateShoppingListResultDto>> handler,
			CancellationToken cancellationToken)
		{
			var command = new GenerateShoppingListCommand(WeekIdentifier.From(year, weekNumber));
			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/{srcYear:int}/w/{srcWeek:int}/copy-to/{dstYear:int}/{dstWeek:int}", CopyPlanToWeek)
			.WithName("CopyPlanToWeek")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status404NotFound)
			.ProducesProblem(StatusCodes.Status422UnprocessableEntity);

		static async Task<IResult> CopyPlanToWeek(
			int srcYear,
			int srcWeek,
			int dstYear,
			int dstWeek,
			ICommandHandler<CopyPlanToWeekCommand, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var source = WeekIdentifier.From(srcYear, srcWeek);
			var target = WeekIdentifier.From(dstYear, dstWeek);
			var result = await handler.HandleAsync(new CopyPlanToWeekCommand(source, target), cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/history/search", SearchHistory)
			.WithName("SearchMealHistory")
			.WithTags("MealPlan")
			.Produces<PagedResult<MealHistoryEntryDto>>();

		static async Task<IResult> SearchHistory(
			IQueryHandler<SearchMealHistoryQuery, Result<PagedResult<MealHistoryEntryDto>>> handler,
			CancellationToken cancellationToken,
			int page = PagingOptions.DefaultPage,
			int pageSize = PagingOptions.DefaultPageSize,
			string? term = null)
		{
			var query = new SearchMealHistoryQuery(PagingOptions.From(page, pageSize), SearchTerm.FromNullable(term));
			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		group.MapSlotEndpoints();

		return app;
	}
}
