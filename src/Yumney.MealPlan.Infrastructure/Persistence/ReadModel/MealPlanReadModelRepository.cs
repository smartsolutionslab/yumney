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
			.FirstOrDefaultAsync(weekRow => weekRow.OwnerId == ownerId && weekRow.Week == weekValue, cancellationToken);

		if (weekItem is null)
		{
			return new WeeklyPlanDto(weekValue, false, EmptyDinnerSlots(SlotServings.DefaultValue));
		}

		var slotRows = await context.MealPlanSlotReadItems
			.Where(slot => slot.OwnerId == ownerId && slot.Week == weekValue)
			.ToListAsync(cancellationToken);

		var visible = weekItem.IsExtendedMode
			? slotRows
			: slotRows.Where(slot => slot.MealType == MealType.Dinner.ToString()).ToList();

		return new WeeklyPlanDto(weekValue, weekItem.IsExtendedMode, visible.ToDtos());
	}

	public async Task<WeeklyPlannedRecipesDto> GetPlannedRecipesAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		var ownerId = owner.Value;
		var weekValue = week.Value;
		var recipeContent = SlotContentType.Recipe.ToString();

		var slotRows = await context.MealPlanSlotReadItems
			.Where(slot => slot.OwnerId == ownerId
				&& slot.Week == weekValue
				&& slot.ContentType == recipeContent
				&& slot.RecipeIdentifier != null)
			.ToListAsync(cancellationToken);

		return new WeeklyPlannedRecipesDto(weekValue, slotRows.ToPlannedRecipeDtos());
	}

	public async Task<IReadOnlyList<MealHistoryEntryDto>> SearchCookedHistoryAsync(OwnerIdentifier owner, string? term, int limit, CancellationToken cancellationToken = default)
	{
		var ownerId = owner.Value;
		var cookedState = MealState.Cooked.ToString();

		var query = context.MealPlanSlotReadItems
			.Where(slot => slot.OwnerId == ownerId && slot.State == cookedState && slot.RecipeTitle != null);

		if (!string.IsNullOrWhiteSpace(term))
		{
			var pattern = $"%{term.Trim()}%";
			query = query.Where(slot => EF.Functions.ILike(slot.RecipeTitle!, pattern));
		}

		var rows = await query
			.OrderByDescending(slot => slot.Week)
			.ThenBy(slot => slot.Day)
			.ThenBy(slot => slot.MealType)
			.Take(limit)
			.ToListAsync(cancellationToken);

		return rows.ToHistoryEntryDtos();
	}

	private static List<MealSlotDto> EmptyDinnerSlots(int defaultServings) =>
		allDays
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
