using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;

/// <summary>
/// Records a "recipe deleted" entry. The recipe title is no longer available
/// here (the published event carries only the identifier), so the activity
/// row stores the identifier with a null title — UI shows "Deleted recipe".
/// </summary>
public sealed class RecipeDeletedActivityHandler(IUserActivityRepository activities)
	: IIntegrationEventHandler<RecipeDeletedIntegrationEvent>
{
	public async Task HandleAsync(RecipeDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var entry = UserActivity.Record(
			OwnerIdentifier.From(@event.OwnerId),
			ActivityType.RecipeDeleted,
			RecipeIdentifierSnapshot.From(@event.RecipeIdentifier));
		await activities.AddAsync(entry, cancellationToken);
	}
}
