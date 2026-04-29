using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace SmartSolutionsLab.Yumney.AppHost;

/// <summary>
/// Run-mode dashboard entries that re-use <c>Yumney.MigrationRunner</c> as a
/// one-shot worker for operational maintenance. Each resource sits idle in the
/// dashboard (<see cref="ResourceBuilderExtensions.WithExplicitStart"/>) until
/// the developer clicks <b>Start</b>; the worker reads its mode from the
/// configured environment variable, runs, and exits.
/// </summary>
internal static class DashboardResetEntries
{
	public static void AddShoppingAndMealPlanResetEntries(
		IDistributedApplicationBuilder builder,
		IResourceBuilder<IResourceWithConnectionString> recipesDb,
		IResourceBuilder<IResourceWithConnectionString> shoppingDb,
		IResourceBuilder<IResourceWithConnectionString> usersDb,
		IResourceBuilder<IResourceWithConnectionString> mealplanDb)
	{
		IResourceBuilder<IResourceWithConnectionString>[] allDbs = [recipesDb, shoppingDb, usersDb, mealplanDb];

		// Drops and re-migrates mealplandb — wipes the event-sourced MealPlan store.
		Add(builder, "yumney-mealplan-reset", "Persistence__ResetMealPlanOnly", mealplanDb, allDbs);

		// Truncates the ShoppingList projection tables and replays the event store
		// into them. Events, metadata, and the legacy ShoppingLists table are
		// untouched. Idempotent.
		Add(builder, "yumney-shopping-projection-reset", "Persistence__RebuildShoppingProjections", shoppingDb, allDbs);

		// Synthesises events for any legacy ShoppingLists rows that don't yet have
		// an entry in the event store. One-off; idempotent for already-backfilled
		// lists.
		Add(builder, "yumney-shopping-event-backfill", "Persistence__BackfillShoppingEvents", shoppingDb, allDbs);
	}

	private static void Add(
		IDistributedApplicationBuilder builder,
		string resourceName,
		string flagName,
		IResourceBuilder<IResourceWithConnectionString> waitFor,
		IResourceBuilder<IResourceWithConnectionString>[] references)
	{
		var resource = builder.AddProject<Projects.Yumney_MigrationRunner>(resourceName);
		foreach (var reference in references)
		{
			resource = resource.WithReference(reference);
		}

		resource.WaitFor(waitFor)
			.WithEnvironment(flagName, "true")
			.WithExplicitStart();
	}
}
