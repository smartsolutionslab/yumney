using SmartSolutionsLab.Yumney.MealPlan.Api.Requests;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.MealPlan.Api;

public static class MealPlanEndpoints
{
    public static IEndpointRouteBuilder MapMealPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/meal-plans");

        group.MapGet("/{year:int}/w/{weekNumber:int}", GetWeeklyPlanAsync)
            .WithName("GetWeeklyPlan")
            .WithTags("MealPlan")
            .Produces<WeeklyPlanDto>();

        group.MapPost("/{year:int}/w/{weekNumber:int}/slots", AssignRecipeAsync)
            .WithName("AssignRecipe")
            .WithTags("MealPlan")
            .Produces<WeeklyPlanDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{year:int}/w/{weekNumber:int}/extended-mode", ToggleExtendedModeAsync)
            .WithName("ToggleExtendedMode")
            .WithTags("MealPlan")
            .Produces<WeeklyPlanDto>();

        group.MapPut("/{year:int}/w/{weekNumber:int}/slots/servings", AdjustServingsAsync)
            .WithName("AdjustSlotServings")
            .WithTags("MealPlan")
            .Produces<WeeklyPlanDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{year:int}/w/{weekNumber:int}/cook-with-leftovers", CookWithLeftoversAsync)
            .WithName("CookWithLeftovers")
            .WithTags("MealPlan")
            .Produces<WeeklyPlanDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{year:int}/w/{weekNumber:int}/planned-recipes", GetPlannedRecipesAsync)
            .WithName("GetPlannedRecipes")
            .WithTags("MealPlan")
            .Produces<WeeklyPlannedRecipesDto>();

        group.MapPut("/{year:int}/w/{weekNumber:int}/slots/swap", SwapSlotsAsync)
            .WithName("SwapMealSlots")
            .WithTags("MealPlan")
            .Produces<WeeklyPlanDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{year:int}/w/{weekNumber:int}/slots/confirm", ConfirmMealAsync)
            .WithName("ConfirmMeal")
            .WithTags("MealPlan")
            .Produces<WeeklyPlanDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{year:int}/w/{weekNumber:int}/slots", ClearSlotAsync)
            .WithName("ClearMealSlot")
            .WithTags("MealPlan")
            .Produces<WeeklyPlanDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetWeeklyPlanAsync(
        int year,
        int weekNumber,
        IQueryHandler<GetWeeklyPlanQuery, Result<WeeklyPlanDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetWeeklyPlanQuery(year, weekNumber);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> AssignRecipeAsync(
        int year,
        int weekNumber,
        AssignRecipeRequest request,
        ICommandHandler<AssignRecipeCommand, Result<WeeklyPlanDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new AssignRecipeCommand(
            year,
            weekNumber,
            request.Day,
            request.RecipeIdentifier,
            request.RecipeTitle,
            request.MealType,
            request.Servings);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> ToggleExtendedModeAsync(
        int year,
        int weekNumber,
        ToggleExtendedModeRequest request,
        ICommandHandler<ToggleExtendedModeCommand, Result<WeeklyPlanDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ToggleExtendedModeCommand(year, weekNumber, request.Enable);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> AdjustServingsAsync(
        int year,
        int weekNumber,
        AdjustServingsRequest request,
        ICommandHandler<AdjustSlotServingsCommand, Result<WeeklyPlanDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new AdjustSlotServingsCommand(year, weekNumber, request.Day, request.MealType, request.Servings);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> CookWithLeftoversAsync(
        int year,
        int weekNumber,
        CookWithLeftoversRequest request,
        ICommandHandler<CookWithLeftoversCommand, Result<WeeklyPlanDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CookWithLeftoversCommand(
            year,
            weekNumber,
            request.CookDay,
            request.RecipeIdentifier,
            request.RecipeTitle,
            request.TotalServings,
            request.EatServings,
            request.LeftoverDay,
            request.MealType);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> GetPlannedRecipesAsync(
        int year,
        int weekNumber,
        IQueryHandler<GetPlannedRecipesQuery, Result<WeeklyPlannedRecipesDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetPlannedRecipesQuery(year, weekNumber);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> SwapSlotsAsync(
        int year,
        int weekNumber,
        SwapSlotsRequest request,
        ICommandHandler<SwapMealSlotsCommand, Result<WeeklyPlanDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SwapMealSlotsCommand(year, weekNumber, request.SourceDay, request.TargetDay, request.MealType);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> ConfirmMealAsync(
        int year,
        int weekNumber,
        ConfirmMealRequest request,
        ICommandHandler<ConfirmMealCommand, Result<WeeklyPlanDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmMealCommand(year, weekNumber, request.Day, request.MealType, request.State);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> ClearSlotAsync(
        int year,
        int weekNumber,
        ClearSlotRequest request,
        ICommandHandler<ClearMealSlotCommand, Result<WeeklyPlanDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ClearMealSlotCommand(year, weekNumber, request.Day, request.MealType);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }
}
