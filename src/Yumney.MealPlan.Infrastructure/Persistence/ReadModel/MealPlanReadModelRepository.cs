using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

public sealed class MealPlanReadModelRepository(MealPlanReadDbContext context) : IMealPlanReadModelRepository
{
#pragma warning disable SA1311
	private static readonly DayOfWeek[] allDays =
	[
		DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
		DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
	];
#pragma warning restore SA1311

	public async Task<WeeklyPlanDto> GetByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		var ownerId = owner.Value;
		var weekValue = week.Value;

		var weekItem = await context.MealPlanWeekReadItems
			.FirstOrDefaultAsync(w => w.OwnerId == ownerId && w.Week == weekValue, cancellationToken);

		if (weekItem is null)
		{
			return new WeeklyPlanDto(weekValue, false, EmptyDinnerSlots(SlotServings.DefaultValue));
		}

		var slotRows = await context.MealPlanSlotReadItems
			.Where(s => s.OwnerId == ownerId && s.Week == weekValue)
			.ToListAsync(cancellationToken);

		var visible = weekItem.IsExtendedMode
			? slotRows
			: slotRows.Where(s => s.MealType == MealType.Dinner.ToString()).ToList();

		var slotDtos = visible
			.Select(ToDto)
			.OrderBy(d => Enum.Parse<DayOfWeek>(d.Day))
			.ThenBy(d => Enum.Parse<MealType>(d.MealType))
			.ToList();

		return new WeeklyPlanDto(weekValue, weekItem.IsExtendedMode, slotDtos);
	}

	public async Task<WeeklyPlannedRecipesDto> GetPlannedRecipesAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		var ownerId = owner.Value;
		var weekValue = week.Value;
		var recipeContent = SlotContentType.Recipe.ToString();

		var slotRows = await context.MealPlanSlotReadItems
			.Where(s => s.OwnerId == ownerId
				&& s.Week == weekValue
				&& s.ContentType == recipeContent
				&& s.RecipeIdentifier != null)
			.ToListAsync(cancellationToken);

		var recipes = slotRows
			.Select(s => new PlannedRecipeDto(
				s.RecipeIdentifier!.Value,
				s.RecipeTitle ?? string.Empty,
				s.Servings,
				s.Day,
				s.MealType))
			.ToList();

		return new WeeklyPlannedRecipesDto(weekValue, recipes);
	}

	private static MealSlotDto ToDto(MealPlanSlotReadItem row) =>
		new(
			row.Day,
			row.MealType,
			row.ContentType,
			row.State,
			row.RecipeIdentifier,
			row.RecipeTitle,
			row.Servings,
			row.FreetextLabel,
			row.LeftoverLabel,
			row.LeftoverSourceDay,
			row.LeftoverSourceMealType,
			row.ContentType == SlotContentType.Empty.ToString());

	private static List<MealSlotDto> EmptyDinnerSlots(int defaultServings)
	{
		return allDays
			.Select(day => new MealSlotDto(
				day.ToString(),
				MealType.Dinner.ToString(),
				SlotContentType.Empty.ToString(),
				MealState.Planned.ToString(),
				null,
				null,
				defaultServings,
				null,
				null,
				null,
				null,
				true))
			.ToList();
	}
}
