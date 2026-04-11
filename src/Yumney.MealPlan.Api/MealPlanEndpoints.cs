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
            request.Servings);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }
}
