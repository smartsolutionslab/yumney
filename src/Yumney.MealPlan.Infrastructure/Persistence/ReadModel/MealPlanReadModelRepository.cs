using System.Globalization;
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

	public async Task<IReadOnlyList<AnalyticsSlotProjection>> GetSlotsInPeriodAsync(
		OwnerIdentifier owner,
		DateOnly periodStart,
		DateOnly periodEndExclusive,
		CancellationToken cancellationToken = default)
	{
		var ownerId = owner.Value;
		var (firstWeek, lastWeek) = SlotWeekBounds(periodStart, periodEndExclusive);
		var cookedState = MealState.Cooked.ToString();
		var skippedState = MealState.Skipped.ToString();

		var rows = await context.MealPlanSlotReadItems
			.AsNoTracking()
			.Where(slot => slot.OwnerId == ownerId
				&& (slot.State == cookedState || slot.State == skippedState)
				&& string.Compare(slot.Week, firstWeek, StringComparison.Ordinal) >= 0
				&& string.Compare(slot.Week, lastWeek, StringComparison.Ordinal) <= 0)
			.Select(slot => new RawSlot(slot.Week, slot.Day, slot.State, slot.RecipeIdentifier, slot.RecipeTitle))
			.ToListAsync(cancellationToken);

		var projections = new List<AnalyticsSlotProjection>(rows.Count);
		foreach (var row in rows)
		{
			var date = SlotDate(row.Week, row.Day);
			if (date < periodStart || date >= periodEndExclusive) continue;
			projections.Add(new AnalyticsSlotProjection(row.RecipeIdentifier, row.RecipeTitle, row.State, date));
		}

		return projections;
	}

	public async Task<IReadOnlyDictionary<Guid, DateOnly>> GetFirstCookDatesAsync(
		OwnerIdentifier owner,
		IReadOnlyList<Guid> recipeIdentifiers,
		CancellationToken cancellationToken = default)
	{
		if (recipeIdentifiers.Count == 0) return new Dictionary<Guid, DateOnly>();

		var ownerId = owner.Value;
		var cookedState = MealState.Cooked.ToString();
		var ids = recipeIdentifiers.ToList();

		var rows = await context.MealPlanSlotReadItems
			.AsNoTracking()
			.Where(slot => slot.OwnerId == ownerId
				&& slot.State == cookedState
				&& slot.RecipeIdentifier.HasValue
				&& ids.Contains(slot.RecipeIdentifier.Value))
			.Select(slot => new { slot.RecipeIdentifier, slot.Week, slot.Day })
			.ToListAsync(cancellationToken);

		var earliest = new Dictionary<Guid, DateOnly>();
		foreach (var row in rows)
		{
			if (row.RecipeIdentifier is not { } id) continue;
			var date = SlotDate(row.Week, row.Day);
			if (!earliest.TryGetValue(id, out var existing) || date < existing)
			{
				earliest[id] = date;
			}
		}

		return earliest;
	}

	private static (string FirstWeek, string LastWeek) SlotWeekBounds(DateOnly periodStart, DateOnly periodEndExclusive)
	{
		// A meal scheduled near the edge of the period might live in the ISO week
		// just before periodStart or just after periodEndExclusive (because ISO
		// weeks aren't aligned to calendar months/years). Bracket by ±1 week so
		// the SQL filter is conservative; the in-memory date filter trims to the
		// exact period.
		var inclusiveLast = periodEndExclusive.AddDays(-1);
		var first = WeekIdentifier.FromDate(periodStart.AddDays(-7)).Value;
		var last = WeekIdentifier.FromDate(inclusiveLast.AddDays(7)).Value;
		return (first, last);
	}

	private static DateOnly SlotDate(string weekValue, string dayValue)
	{
		var dashIndex = weekValue.IndexOf('-', StringComparison.Ordinal);
		var year = int.Parse(weekValue[..dashIndex], CultureInfo.InvariantCulture);
		var weekNumber = int.Parse(weekValue[(dashIndex + 2)..], CultureInfo.InvariantCulture);
		var dayOfWeek = Enum.Parse<DayOfWeek>(dayValue);
		var monday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday);
		var offset = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
		return DateOnly.FromDateTime(monday.AddDays(offset));
	}

#pragma warning disable SA1402, SA1649
	private sealed record RawSlot(string Week, string Day, string State, Guid? RecipeIdentifier, string? RecipeTitle);
#pragma warning restore SA1402, SA1649

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
