namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

/// <summary>
/// Hard-deletes every MealPlan-module row owned by the given user — both event
/// store (events + aggregate metadata) and read models. Used by the GDPR
/// account-deletion flow (US-101). Implementations MUST be idempotent.
/// </summary>
public interface IMealPlanUserDataPurger
{
	Task PurgeAsync(string keycloakUserId, CancellationToken cancellationToken = default);
}
