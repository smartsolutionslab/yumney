using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.IntegrationEventHandlers;

/// <summary>
/// GDPR Art. 17 reaction (US-101). Wipes every WeeklyPlan (events + metadata
/// + read models) owned by the deleted user.
/// </summary>
public sealed class UserAccountDeletedHandler(IMealPlanUserDataPurger purger)
	: IIntegrationEventHandler<UserAccountDeletedIntegrationEvent>
{
	public Task HandleAsync(UserAccountDeletedIntegrationEvent @event, CancellationToken cancellationToken = default) =>
		purger.PurgeAsync(OwnerIdentifier.From(@event.KeycloakUserId), cancellationToken);
}
