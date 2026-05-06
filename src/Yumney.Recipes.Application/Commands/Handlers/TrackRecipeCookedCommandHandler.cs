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
			return Result.Failure(DeleteRecipeErrors.AccessDenied);
		}

		await eventBus.PublishAsync(
			new RecipeCookedIntegrationEvent(recipe.Owner.Value, recipe.Id.Value, recipe.Title.Value),
			cancellationToken);

		return Result.Success();
	}
}
