namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Consumer-defined contract for confirming a planned meal's state (cooked,
/// skipped, restored to planned) in the user's weekly meal plan. Owned by
/// Recipes (the chat surface is the consumer) per CLAUDE.md cross-module rule.
/// </summary>
public interface IMealConfirmation
{
	/// <summary>Confirm a meal slot's state.</summary>
	/// <param name="request">Year, week, day, meal type, target state.</param>
	/// <param name="cancellationToken">Cancellation propagated to the call.</param>
	/// <returns>True if the confirmation succeeded, false if the upstream rejected it.</returns>
	Task<bool> ConfirmAsync(ConfirmMealRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Consumer-flavored shape of a confirm-meal request.</summary>
/// <param name="Year">ISO year.</param>
/// <param name="WeekNumber">ISO week number 1-53.</param>
/// <param name="Day">English weekday name (Monday..Sunday).</param>
/// <param name="MealType">One of "Breakfast", "Lunch", "Dinner", "Snack".</param>
/// <param name="State">One of "Planned", "Cooked", "Skipped".</param>
public sealed record ConfirmMealRequest(int Year, int WeekNumber, string Day, string MealType, string State);
