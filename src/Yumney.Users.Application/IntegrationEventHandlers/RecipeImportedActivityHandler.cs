using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;

/// <summary>
/// Records a "recipe imported" entry in the user's activity log (US-121).
/// </summary>
public sealed class RecipeImportedActivityHandler(IUserActivityRepository activities)
	: IIntegrationEventHandler<RecipeImportedIntegrationEvent>
{
	public async Task HandleAsync(RecipeImportedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var entry = UserActivity.Record(
			OwnerIdentifier.From(@event.OwnerId),
			ActivityType.RecipeImported,
			RecipeIdentifierSnapshot.From(@event.RecipeIdentifier),
			RecipeTitleSnapshot.From(@event.RecipeTitle));
		await activities.AddAsync(entry, cancellationToken);
	}
}
