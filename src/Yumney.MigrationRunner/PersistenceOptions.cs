namespace SmartSolutionsLab.Yumney.MigrationRunner;

/// <summary>
/// Toggles that switch <see cref="MigrationWorker"/> from its default "apply
/// pending migrations across every module" path into one of the operational
/// modes driven by the Aspire dashboard reset entries
/// (<c>DashboardResetEntries</c>).
///
/// Bound from configuration section <c>Persistence</c>. Each dashboard entry
/// flips exactly one flag to <c>true</c> via an environment variable; the
/// regular CI / staging deploy leaves them all <c>false</c>.
/// </summary>
public sealed class PersistenceOptions
{
	public const string SectionName = "Persistence";

	/// <summary>
	/// Gets a value indicating whether the ShoppingList projection tables should
	/// be truncated and rebuilt from the event store. Events and metadata are
	/// untouched. Idempotent.
	/// </summary>
	public bool RebuildShoppingProjections { get; init; }

	/// <summary>
	/// Gets a value indicating whether the MealPlan database should be dropped
	/// and re-migrated. Wipes the event-sourced MealPlan store. Destructive —
	/// only exposed via the Aspire dashboard reset entry.
	/// </summary>
	public bool ResetMealPlanOnly { get; init; }

	/// <summary>
	/// Gets a value indicating whether the Shopping database should be dropped
	/// and re-migrated. Wipes events, metadata, projections, and legacy lists in
	/// one shot. Destructive — only exposed via the Aspire dashboard reset entry.
	/// </summary>
	public bool ResetShoppingOnly { get; init; }
}
