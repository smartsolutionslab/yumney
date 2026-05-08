using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class TrackRecipeCookedCommandHandler(IRecipesUnitOfWork unitOfWork, ICurrentUser currentUser, IEventBus eventBus)
	: ICommandHandler<TrackRecipeCookedCommand, Result>
{
	public async Task<Result> HandleAsync(TrackRecipeCookedCommand command, CancellationToken cancellationToken = default)
	{
		var recipe = await unitOfWork.Recipes.GetByIdAsync(command.Identifier, cancellationToken);

		if (recipe.Owner != currentUser.AsOwner())
		{
			return Result.Failure(TrackRecipeCookedErrors.AccessDenied);
		}

		// PublishAsync stages on the outbox; SaveChangesAsync commits the
		// staged row even though no entity changes occurred. Without the save,
		// the outbox row would never persist and the message would be lost.
		await eventBus.PublishAsync(
			new RecipeCookedIntegrationEvent(recipe.Owner.Value, recipe.Id.Value, recipe.Title.Value),
			cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success();
	}
}
