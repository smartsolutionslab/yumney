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

	/// <summary>Assign a recipe to a slot in the given week. POST /api/v1/meal-plans/{year}/w/{weekNumber}/slots.</summary>
	/// <param name="year">ISO year.</param>
	/// <param name="weekNumber">ISO week number.</param>
	/// <param name="body">Day, recipe, meal type, optional servings.</param>
	/// <param name="cancellationToken">Cancellation propagated to the HTTP call.</param>
	/// <returns>True on 2xx, false on any error.</returns>
	Task<bool> AssignRecipeAsync(int year, int weekNumber, AssignRecipeBody body, CancellationToken cancellationToken = default);

	/// <summary>Confirm a meal slot's state (cooked / skipped / restored). PUT /api/v1/meal-plans/{year}/w/{weekNumber}/slots/confirm.</summary>
	/// <param name="year">ISO year.</param>
	/// <param name="weekNumber">ISO week number.</param>
	/// <param name="body">Day, meal type, target state.</param>
	/// <param name="cancellationToken">Cancellation propagated to the HTTP call.</param>
	/// <returns>True on 2xx, false on any error.</returns>
	Task<bool> ConfirmMealAsync(int year, int weekNumber, ConfirmMealBody body, CancellationToken cancellationToken = default);

	/// <summary>Swap two meal slots. PUT /api/v1/meal-plans/{year}/w/{weekNumber}/slots/swap.</summary>
	/// <param name="year">ISO year.</param>
	/// <param name="weekNumber">ISO week number.</param>
	/// <param name="body">Source day, target day, meal type.</param>
	/// <param name="cancellationToken">Cancellation propagated to the HTTP call.</param>
	/// <returns>True on 2xx, false on any error.</returns>
	Task<bool> SwapSlotsAsync(int year, int weekNumber, SwapSlotsBody body, CancellationToken cancellationToken = default);

	/// <summary>Clear a meal slot. DELETE /api/v1/meal-plans/{year}/w/{weekNumber}/slots.</summary>
	/// <param name="year">ISO year.</param>
	/// <param name="weekNumber">ISO week number.</param>
	/// <param name="body">Day + meal type identifying the slot.</param>
	/// <param name="cancellationToken">Cancellation propagated to the HTTP call.</param>
	/// <returns>True on 2xx, false on any error.</returns>
	Task<bool> ClearSlotAsync(int year, int weekNumber, ClearSlotBody body, CancellationToken cancellationToken = default);
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

/// <summary>Body for the assign-recipe POST.</summary>
public sealed record AssignRecipeBody(
	string Day,
	Guid RecipeIdentifier,
	string RecipeTitle,
	string MealType,
	int? Servings);

/// <summary>Body for the confirm-meal PUT.</summary>
public sealed record ConfirmMealBody(string Day, string MealType, string State);

/// <summary>Body for the swap-slots PUT.</summary>
public sealed record SwapSlotsBody(string SourceDay, string TargetDay, string MealType);

/// <summary>Body for the clear-slot DELETE.</summary>
public sealed record ClearSlotBody(string Day, string MealType);
