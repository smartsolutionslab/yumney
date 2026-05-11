namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>Consumer-defined contract for clearing a meal slot from the weekly plan.</summary>
public interface IMealSlotClearer
{
	/// <summary>Clear (cancel) the meal planned for a given day + meal type.</summary>
	/// <param name="request">Year, week, day, meal type.</param>
	/// <param name="cancellationToken">Cancellation propagated to the call.</param>
	/// <returns>True on success, false if the upstream rejected the request.</returns>
	Task<bool> ClearAsync(ClearMealSlotRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Consumer-flavored shape of a clear-meal-slot request.</summary>
/// <param name="Year">ISO year.</param>
/// <param name="WeekNumber">ISO week number 1-53.</param>
/// <param name="Day">English weekday name.</param>
/// <param name="MealType">One of "Breakfast", "Lunch", "Dinner", "Snack".</param>
public sealed record ClearMealSlotRequest(int Year, int WeekNumber, string Day, string MealType);
