using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

public sealed class GetPlannedRecipesQueryHandler(
	IWeeklyPlanRepository plans,
	ICurrentUser currentUser) : IQueryHandler<GetPlannedRecipesQuery, Result<WeeklyPlannedRecipesDto>>
{
	public async Task<Result<WeeklyPlannedRecipesDto>> HandleAsync(GetPlannedRecipesQuery query, CancellationToken cancellationToken = default)
	{
		var week = query.Week;
		var owner = currentUser.AsOwner();

		var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);
		if (plan is null) return new WeeklyPlannedRecipesDto(week.Value, []);

		var recipes = plan.Slots
			.Where(s => s.ContentType == SlotContentType.Recipe && s.Recipe is not null)
			.Select(s => new PlannedRecipeDto(
				s.Recipe!.RecipeIdentifier,
				s.Recipe.Title,
				s.Servings.Value,
				s.Day.ToString(),
				s.MealType.ToString()))
			.ToList();

		return new WeeklyPlannedRecipesDto(week.Value, recipes);
	}
}
