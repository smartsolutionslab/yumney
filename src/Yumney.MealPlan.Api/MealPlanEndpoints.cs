using Microsoft.AspNetCore.Mvc;
using SmartSolutionsLab.Yumney.MealPlan.Api.Requests;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
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
            AssignRecipeRequest request,
            ICommandHandler<AssignRecipeCommand, Result<WeeklyPlanDto>> handler,
            CancellationToken cancellationToken)
        {
            var week = WeekIdentifier.From(year, weekNumber);
            var (day, recipeIdentifier, recipeTitle, mealType, servings) = request;
            var command = new AssignRecipeCommand(week, day, recipeIdentifier, recipeTitle, mealType, servings);

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
            ToggleExtendedModeRequest request,
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
            AdjustServingsRequest request,
            ICommandHandler<AdjustSlotServingsCommand, Result<WeeklyPlanDto>> handler,
            CancellationToken cancellationToken)
        {
            var week = WeekIdentifier.From(year, weekNumber);
            var (day, mealType, servings) = request;
            var command = new AdjustSlotServingsCommand(week, day, mealType, servings);

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

        return app;

        static async Task<IResult> CookWithLeftovers(
            int year,
            int weekNumber,
            CookWithLeftoversRequest request,
            ICommandHandler<CookWithLeftoversCommand, Result<WeeklyPlanDto>> handler,
            CancellationToken cancellationToken)
        {
            var week = WeekIdentifier.From(year, weekNumber);
            var (cookDay, recipeIdentifier, recipeTitle, totalServings, eatServings, leftoverDay, mealType) = request;
            var command = new CookWithLeftoversCommand(week, cookDay, recipeIdentifier, recipeTitle, totalServings, eatServings, leftoverDay, mealType);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToOk();
        }

        static async Task<IResult> ClearSlot(
            int year,
            int weekNumber,
            [FromBody] ClearSlotRequest request,
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
            SwapSlotsRequest request,
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
            ConfirmMealRequest request,
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
            var result = await handler.HandleAsync(new GenerateShoppingListCommand(week), cancellationToken);
            return result.ToOk();
        }
    }
}
