using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

namespace SmartSolutionsLab.Yumney.MealPlan.Api;

#pragma warning disable SA1601
public static partial class MealPlanEndpoints
#pragma warning restore SA1601
{
	private static RouteGroupBuilder MapSlotEndpoints(this RouteGroupBuilder group)
	{
		group.MapPost("/{year:int}/w/{weekNumber:int}/slots", AssignRecipe)
			.WithName("AssignRecipe")
			.WithTags("MealPlan")
			.WithCapability(
				name: "assign_meal",
				description: "Plan a recipe into a meal slot for an ISO week ('plan carbonara for Wednesday', 'add risotto to Friday lunch'). Requires a recipe identifier from a prior recipe lookup.",
				surfaces: CapabilitySurface.Chat | CapabilitySurface.Mcp)
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
			var slotServings = SlotServings.FromNullable(servings);
			var command = new AssignRecipeCommand(week, day, recipe, mealType, slotServings);

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

		group.MapPut("/{year:int}/w/{weekNumber:int}/slots/swap", SwapSlots)
			.WithName("SwapMealSlots")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

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

		group.MapPut("/{year:int}/w/{weekNumber:int}/slots/confirm", ConfirmMeal)
			.WithName("ConfirmMeal")
			.WithTags("MealPlan")
			.WithCapability(
				name: "confirm_meal_cooked",
				description: "Update a planned meal's state to Cooked, Skipped, or Planned ('I made the spaghetti on Wednesday', 'skip Friday's dinner').",
				surfaces: CapabilitySurface.Chat | CapabilitySurface.Mcp)
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

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

		group.MapDelete("/{year:int}/w/{weekNumber:int}/slots", ClearSlot)
			.WithName("ClearMealSlot")
			.WithTags("MealPlan")
			.Produces<WeeklyPlanDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

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

		return group;
	}
}
