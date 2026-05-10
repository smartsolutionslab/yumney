namespace SmartSolutionsLab.Yumney.MealPlan.Client;

/// <summary>
/// Slim transport-shaped HTTP client over the MealPlan module's REST endpoints.
/// Used by sibling modules (Recipes, Shopping, …) per the consumer-defined-contracts
/// pattern — each consumer wraps relevant calls in its own <c>I*Provider</c> interface.
/// </summary>
public interface IMealPlanClient
{
	/// <summary>Fetch the weekly plan for the given ISO week.</summary>
	/// <param name="year">ISO year, e.g. 2026.</param>
	/// <param name="weekNumber">ISO week number 1-53.</param>
	/// <param name="cancellationToken">Cancellation propagated to the HTTP call.</param>
	/// <returns>The weekly plan or null if the week has not been planned.</returns>
	Task<WeeklyPlanResponse?> GetWeeklyPlanAsync(int year, int weekNumber, CancellationToken cancellationToken = default);
}

/// <summary>Trimmed weekly-plan shape returned over the wire.</summary>
public sealed record WeeklyPlanResponse(
	string Week,
	bool IsExtendedMode,
	IReadOnlyList<WeeklyPlanSlotResponse> Slots);

/// <summary>One filled or empty slot in the weekly plan.</summary>
public sealed record WeeklyPlanSlotResponse(
	string Day,
	string MealType,
	string ContentType,
	string State,
	Guid? RecipeIdentifier,
	string? RecipeTitle,
	int Servings,
	bool IsEmpty);
