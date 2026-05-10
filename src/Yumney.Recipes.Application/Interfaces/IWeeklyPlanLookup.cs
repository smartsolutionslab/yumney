namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Consumer-defined contract for fetching the user's weekly meal plan from the
/// MealPlan module. Lives here (not in MealPlan) because the chat surface is the
/// consumer — see CLAUDE.md "Cross-Module Dependencies" + ADR 0002.
/// </summary>
public interface IWeeklyPlanLookup
{
	/// <summary>Get the weekly plan for the given ISO week, or null if not yet planned.</summary>
	/// <param name="year">ISO year, e.g. 2026.</param>
	/// <param name="weekNumber">ISO week number 1-53.</param>
	/// <param name="cancellationToken">Cancellation propagated to the lookup.</param>
	/// <returns>Consumer-flavored weekly plan, or null if the week has not been planned.</returns>
	Task<WeeklyPlanLookupResult?> GetForWeekAsync(int year, int weekNumber, CancellationToken cancellationToken = default);
}

/// <summary>Trimmed weekly-plan shape returned to chat tools.</summary>
public sealed record WeeklyPlanLookupResult(
	string Week,
	bool IsExtendedMode,
	IReadOnlyList<WeeklyPlanLookupSlot> Slots);

/// <summary>One slot in the consumer-flavored weekly plan.</summary>
public sealed record WeeklyPlanLookupSlot(
	string Day,
	string MealType,
	Guid? RecipeIdentifier,
	string? RecipeTitle,
	int Servings,
	bool IsEmpty);
