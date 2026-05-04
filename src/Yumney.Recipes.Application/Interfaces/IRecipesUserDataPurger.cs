namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Hard-deletes every Recipes-module row owned by the given user. Used by the
/// GDPR account-deletion flow (US-101). Implementations MUST be idempotent —
/// the integration event that drives this may be re-delivered.
/// </summary>
public interface IRecipesUserDataPurger
{
	Task PurgeAsync(string keycloakUserId, CancellationToken cancellationToken = default);
}
