using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

/// <summary>
/// Hard-deletes every Shopping-module row owned by the given user — both event
/// store (events + aggregate metadata) and read models. Used by the GDPR
/// account-deletion flow (US-101). Implementations MUST be idempotent.
/// </summary>
public interface IShoppingUserDataPurger
{
	Task PurgeAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);
}
