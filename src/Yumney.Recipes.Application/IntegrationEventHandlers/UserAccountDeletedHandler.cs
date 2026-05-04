using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

namespace SmartSolutionsLab.Yumney.Recipes.Application.IntegrationEventHandlers;

/// <summary>
/// GDPR Art. 17 reaction (US-101). Wipes every Recipe and RecipeFavorite owned
/// by the deleted user.
/// </summary>
public sealed class UserAccountDeletedHandler(IRecipesUserDataPurger purger)
	: IIntegrationEventHandler<UserAccountDeletedIntegrationEvent>
{
	public Task HandleAsync(UserAccountDeletedIntegrationEvent @event, CancellationToken cancellationToken = default) =>
		purger.PurgeAsync(@event.KeycloakUserId, cancellationToken);
}
