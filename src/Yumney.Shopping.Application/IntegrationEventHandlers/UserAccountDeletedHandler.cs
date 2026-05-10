using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;

/// <summary>
/// GDPR Art. 17 reaction (US-101). Wipes every shopping list (events + metadata
/// + read models) and every ledger / balance row owned by the deleted user.
/// </summary>
public sealed class UserAccountDeletedHandler(IShoppingUserDataPurger purger)
	: IIntegrationEventHandler<UserAccountDeletedIntegrationEvent>
{
	public Task HandleAsync(UserAccountDeletedIntegrationEvent @event, CancellationToken cancellationToken = default) =>
		purger.PurgeAsync(@event.KeycloakUserId, cancellationToken);
}
