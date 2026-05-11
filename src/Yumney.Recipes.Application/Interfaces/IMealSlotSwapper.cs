namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>Consumer-defined contract for swapping two meal slots in the weekly plan.</summary>
public interface IMealSlotSwapper
{
	/// <summary>Swap the meals planned for two days within the same week.</summary>
	/// <param name="request">Year, week, source / target days, meal type.</param>
	/// <param name="cancellationToken">Cancellation propagated to the call.</param>
	/// <returns>True on success, false if the upstream rejected the request.</returns>
	Task<bool> SwapAsync(SwapMealSlotsRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Consumer-flavored shape of a swap-meal-slots request.</summary>
/// <param name="Year">ISO year.</param>
/// <param name="WeekNumber">ISO week number 1-53.</param>
/// <param name="SourceDay">English weekday name to swap from.</param>
/// <param name="TargetDay">English weekday name to swap to.</param>
/// <param name="MealType">One of "Breakfast", "Lunch", "Dinner", "Snack".</param>
public sealed record SwapMealSlotsRequest(int Year, int WeekNumber, string SourceDay, string TargetDay, string MealType);
