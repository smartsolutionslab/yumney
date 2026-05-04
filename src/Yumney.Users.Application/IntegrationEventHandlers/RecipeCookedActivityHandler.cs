using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;

/// <summary>
/// Records a "recipe cooked" entry. Each completion is a discrete event so
/// the cook count derives directly from the count of these activity rows.
/// </summary>
public sealed class RecipeCookedActivityHandler(IUserActivityRepository activities)
	: IIntegrationEventHandler<RecipeCookedIntegrationEvent>
{
	public async Task HandleAsync(RecipeCookedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var entry = UserActivity.Record(
			OwnerIdentifier.From(@event.OwnerId),
			ActivityType.RecipeCooked,
			RecipeIdentifierSnapshot.From(@event.RecipeIdentifier),
			RecipeTitleSnapshot.From(@event.RecipeTitle));
		await activities.AddAsync(entry, cancellationToken);
	}
}
