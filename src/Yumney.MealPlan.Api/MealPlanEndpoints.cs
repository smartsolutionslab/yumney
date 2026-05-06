using Microsoft.AspNetCore.Mvc;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.MealPlan.Api;

public static class MealPlanEndpoints
{
	public static IEndpointRouteBuilder MapMealPlanEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/meal-plans");

		group.MapGet("/{year:int}/w/{weekNumber:int}", GetWeeklyPlan)
			.WithName("GetWeeklyPlan")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>();

		static async Task<IResult> GetWeeklyPlan(
			int year,
			int weekNumber,
			IQueryHandler<GetWeeklyPlanQuery, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);

			var result = await handler.HandleAsync(new GetWeeklyPlanQuery(week), cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/{year:int}/w/{weekNumber:int}/slots", AssignRecipe)
			.WithName("AssignRecipe")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest);

		static async Task<IResult> AssignRecipe(
			int year,
			int weekNumber,
			Requests.AssignRecipe request,
			ICommandHandler<AssignRecipeCommand, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var (day, recipeIdentifier, recipeTitle, mealType, servings) = request;
			var recipe = SlotRecipeReference.From(recipeIdentifier, recipeTitle);
			SlotServings? slotServings = servings.HasValue ? SlotServings.From(servings.Value) : null;
			var command = new AssignRecipeCommand(week, day, recipe, mealType, slotServings);

			var result = await handler.HandleAsync(command, cancellationToken);
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
			var week = WeekIdentifier.From(year, weekNumber);
			var command = new ToggleExtendedModeCommand(week, request.Enable);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapPut("/{year:int}/w/{weekNumber:int}/slots/servings", AdjustServings)
			.WithName("AdjustSlotServings")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> AdjustServings(
			int year,
			int weekNumber,
			Requests.AdjustServings request,
			ICommandHandler<AdjustSlotServingsCommand, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var (day, mealType, servings) = request;
			var command = new AdjustSlotServingsCommand(week, day, mealType, SlotServings.From(servings));

			var result = await handler.HandleAsync(command, cancellationToken);

			return result.ToOk();
		}

		group.MapPost("/{year:int}/w/{weekNumber:int}/cook-with-leftovers", CookWithLeftovers)
			.WithName("CookWithLeftovers")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest);

		group.MapGet("/{year:int}/w/{weekNumber:int}/planned-recipes", GetPlannedRecipes)
			.WithName("GetPlannedRecipes")
			.WithTags("MealPlan")
			.Produces<WeeklyPlannedRecipesDto>();

		group.MapPut("/{year:int}/w/{weekNumber:int}/slots/swap", SwapSlots)
			.WithName("SwapMealSlots")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

		group.MapPut("/{year:int}/w/{weekNumber:int}/slots/confirm", ConfirmMeal)
			.WithName("ConfirmMeal")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

		group.MapDelete("/{year:int}/w/{weekNumber:int}/slots", ClearSlot)
			.WithName("ClearMealSlot")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

		group.MapPost("/{year:int}/w/{weekNumber:int}/generate-shopping-list", GenerateShoppingList)
			.WithName("GenerateShoppingList")
			.WithTags("MealPlan")
			.Produces<GenerateShoppingListResultDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest);

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
			var query = new SearchMealHistoryQuery(
				PagingOptions.From(page, pageSize),
				SearchTerm.FromNullable(term));
			var result = await handler.HandleAsync(query, cancellationToken);
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

		return app;

		static async Task<IResult> CookWithLeftovers(
			int year,
			int weekNumber,
			Requests.CookWithLeftovers request,
			ICommandHandler<CookWithLeftoversCommand, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var (cookDay, recipeIdentifier, recipeTitle, totalServings, eatServings, leftoverDay, mealType) = request;
			var recipe = SlotRecipeReference.From(recipeIdentifier, recipeTitle);
			var command = new CookWithLeftoversCommand(
				week,
				cookDay,
				recipe,
				SlotServings.From(totalServings),
				SlotServings.From(eatServings),
				leftoverDay,
				mealType);

			var result = await handler.HandleAsync(command, cancellationToken);

			return result.ToOk();
		}

		static async Task<IResult> ClearSlot(
			int year,
			int weekNumber,
			[FromBody] Requests.ClearSlot request,
			ICommandHandler<ClearMealSlotCommand, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var (day, mealType) = request;
			var command = new ClearMealSlotCommand(week, day, mealType);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		static async Task<IResult> GetPlannedRecipes(
			int year,
			int weekNumber,
			IQueryHandler<GetPlannedRecipesQuery, Result<WeeklyPlannedRecipesDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var result = await handler.HandleAsync(new GetPlannedRecipesQuery(week), cancellationToken);
			return result.ToOk();
		}

		static async Task<IResult> SwapSlots(
			int year,
			int weekNumber,
			Requests.SwapSlots request,
			ICommandHandler<SwapMealSlotsCommand, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var (sourceDay, targetDay, mealType) = request;
			var command = new SwapMealSlotsCommand(week, sourceDay, targetDay, mealType);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		static async Task<IResult> ConfirmMeal(
			int year,
			int weekNumber,
			Requests.ConfirmMeal request,
			ICommandHandler<ConfirmMealCommand, Result<WeeklyPlanDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var (day, mealType, state) = request;
			var command = new ConfirmMealCommand(week, day, mealType, state);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		static async Task<IResult> GenerateShoppingList(
			int year,
			int weekNumber,
			ICommandHandler<GenerateShoppingListCommand, Result<GenerateShoppingListResultDto>> handler,
			CancellationToken cancellationToken)
		{
			var week = WeekIdentifier.From(year, weekNumber);
			var command = new GenerateShoppingListCommand(week);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}
	}
}
