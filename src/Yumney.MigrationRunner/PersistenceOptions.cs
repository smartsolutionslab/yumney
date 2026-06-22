namespace SmartSolutionsLab.Yumney.MigrationRunner;

/// <summary>
/// Toggles that switch <see cref="MigrationWorker"/> from its default "apply
/// pending migrations across every module" path into the operational mode
/// driven by the Aspire dashboard reset entry (<c>DashboardResetEntries</c>).
///
/// Bound from configuration section <c>Persistence</c>. The dashboard entry
/// flips the flag to <c>true</c> via an environment variable; the regular
/// CI / staging deploy leaves it <c>false</c>.
/// </summary>
public sealed class PersistenceOptions
{
	public const string SectionName = "Persistence";

	/// <summary>
	/// Gets a value indicating whether the MealPlan database should be dropped
	/// and re-migrated. Wipes the event-sourced MealPlan store. Destructive —
	/// only exposed via the Aspire dashboard reset entry.
	/// </summary>
	public bool ResetMealPlanOnly { get; init; }
}
