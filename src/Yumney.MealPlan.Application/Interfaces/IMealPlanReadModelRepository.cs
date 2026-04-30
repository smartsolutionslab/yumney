using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

/// <summary>
/// Read model repository for the meal plan projection.
/// </summary>
public interface IMealPlanReadModelRepository
{
	/// <summary>
	/// Returns the weekly plan view. If no plan exists, returns a placeholder
	/// view with seven empty Dinner slots so the UI can render the empty week.
	/// </summary>
	/// <param name="owner">The owner identifier.</param>
	/// <param name="week">The week identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The weekly plan view.</returns>
	Task<WeeklyPlanDto> GetByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the planned recipes for the week (Recipe-typed slots only).
	/// Empty result if no plan exists.
	/// </summary>
	/// <param name="owner">The owner identifier.</param>
	/// <param name="week">The week identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The planned recipes for the week.</returns>
	Task<WeeklyPlannedRecipesDto> GetPlannedRecipesAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default);

	/// <summary>
	/// Searches cooked-state slots by recipe title (case-insensitive substring),
	/// newest week first. Empty / null term returns the most recent cooked meals.
	/// </summary>
	/// <param name="owner">The owner identifier.</param>
	/// <param name="term">Optional substring to match against the slot's recipe title.</param>
	/// <param name="limit">Max number of rows to return.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The matching history entries, newest first.</returns>
	Task<IReadOnlyList<MealHistoryEntryDto>> SearchCookedHistoryAsync(OwnerIdentifier owner, string? term, int limit, CancellationToken cancellationToken = default);
}
