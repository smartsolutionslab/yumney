using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

public sealed class MealPlanReadModelRepository(MealPlanReadDbContext context) : IMealPlanReadModelRepository
{
	public async Task<WeeklyPlanDto> GetByOwnerAndWeekAsync(
		OwnerIdentifier owner,
		WeekIdentifier week,
		CancellationToken cancellationToken = default)
	{
		var ownerId = owner.Value;
		var weekValue = week.Value;

		var weekItem = await context.MealPlanWeekReadItems
			.AsNoTracking()
			.FirstOrDefaultAsync(weekRow => weekRow.OwnerId == ownerId && weekRow.Week == weekValue, cancellationToken);

		if (weekItem is null)
		{
			return new WeeklyPlanDto(weekValue, false, EmptyDinnerSlots(SlotServings.DefaultValue));
		}

		var slotRows = await context.MealPlanSlotReadItems
			.AsNoTracking()
			.Where(slot => slot.OwnerId == ownerId && slot.Week == weekValue)
			.ToListAsync(cancellationToken);

		var visible = weekItem.IsExtendedMode
			? slotRows
			: slotRows.Where(slot => slot.MealType == MealType.Dinner.ToString()).ToList();

		return new WeeklyPlanDto(weekValue, weekItem.IsExtendedMode, visible.ToDtos());
	}

	public async Task<WeeklyPlannedRecipesDto> GetPlannedRecipesAsync(
		OwnerIdentifier owner,
		WeekIdentifier week,
		CancellationToken cancellationToken = default)
	{
		var ownerId = owner.Value;
		var weekValue = week.Value;
		var recipeContent = SlotContentType.Recipe.ToString();

		var slotRows = await context.MealPlanSlotReadItems
			.AsNoTracking()
			.Where(slot => slot.OwnerId == ownerId
				&& slot.Week == weekValue
				&& slot.ContentType == recipeContent
				&& slot.RecipeIdentifier != null)
			.ToListAsync(cancellationToken);

		return new WeeklyPlannedRecipesDto(weekValue, slotRows.ToPlannedRecipeDtos());
	}

	public async Task<PagedResult<MealHistoryEntryDto>> SearchCookedHistoryAsync(
		OwnerIdentifier owner,
		SearchTerm? term,
		PagingOptions paging,
		CancellationToken cancellationToken = default)
	{
		var ownerId = owner.Value;
		var cookedState = MealState.Cooked.ToString();

		var query = context.MealPlanSlotReadItems
			.AsNoTracking()
			.Where(slot => slot.OwnerId == ownerId && slot.State == cookedState && slot.RecipeTitle != null);

		if (term is not null)
		{
			var pattern = $"%{term.Value}%";
			query = query.Where(slot => EF.Functions.ILike(slot.RecipeTitle!, pattern));
		}

		var totalCount = await query.CountAsync(cancellationToken);

		// Day and MealType columns store enum names (strings); ordering at SQL would
		// be alphabetical (Friday, Monday, …). Page by Week at SQL, sort precisely
		// in memory after parsing.
		var rows = await query
			.OrderByDescending(slot => slot.Week)
			.Skip(paging.Skip)
			.Take(paging.PageSize.Value)
			.ToListAsync(cancellationToken);

		var sorted = rows
			.OrderByDescending(slot => slot.Week)
			.ThenBy(slot => Enum.Parse<DayOfWeek>(slot.Day))
			.ThenBy(slot => Enum.Parse<MealType>(slot.MealType))
			.ToList();

		return sorted.ToHistoryEntryDtos().AsPagedResult(ItemCount.From(totalCount), paging);
	}

	private static List<MealSlotDto> EmptyDinnerSlots(int defaultServings) =>
		WeekDays.MondayToSunday
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
