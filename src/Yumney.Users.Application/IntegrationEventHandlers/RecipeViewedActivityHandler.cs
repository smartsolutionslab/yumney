using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;

/// <summary>
/// Records a "recipe viewed" entry. The Recipes module already debounces
/// view events with a 5-minute window per (user, recipe), so this handler
/// stays naive and just persists what arrives.
/// </summary>
public sealed class RecipeViewedActivityHandler(IUserActivityRepository activities)
	: IIntegrationEventHandler<RecipeViewedIntegrationEvent>
{
	public async Task HandleAsync(RecipeViewedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var entry = UserActivity.Record(
			OwnerIdentifier.From(@event.OwnerId),
			ActivityType.RecipeViewed,
			RecipeIdentifierSnapshot.From(@event.RecipeIdentifier),
			RecipeTitleSnapshot.From(@event.RecipeTitle));
		await activities.AddAsync(entry, cancellationToken);
	}
}
