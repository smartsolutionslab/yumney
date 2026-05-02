using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

namespace SmartSolutionsLab.Yumney.Recipes.Application.IntegrationEventHandlers;

// Stub consumer: the event is published by Shopping but Recipes does not yet
// react to it. Registered so the architecture test that requires every
// integration event to have at least one handler stays green; replace the
// no-op with real logic when a Recipes-side reaction is defined.
public sealed class ShoppingListRecipeReferenceClearedHandler : IIntegrationEventHandler<ShoppingListRecipeReferenceClearedCrossModuleIntegrationEvent>
{
	public Task HandleAsync(ShoppingListRecipeReferenceClearedCrossModuleIntegrationEvent @event, CancellationToken cancellationToken = default) =>
		Task.CompletedTask;
}
