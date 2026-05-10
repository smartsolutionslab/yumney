using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class DeleteRecipeCommandHandler(IRecipesUnitOfWork unitOfWork, ICurrentUser currentUser, IEventBus eventBus)
	: ICommandHandler<DeleteRecipeCommand, Result>
{
	public async Task<Result> HandleAsync(DeleteRecipeCommand command, CancellationToken cancellationToken = default)
	{
		var identifier = command.Identifier;
		var owner = currentUser.AsOwner();

		var recipe = await unitOfWork.Recipes.GetByIdForUpdateAsync(identifier, cancellationToken);

		if (recipe.Owner != owner)
		{
			return Result.Failure(DeleteRecipeErrors.AccessDenied);
		}

		recipe.MarkAsDeleted();

		unitOfWork.Recipes.Remove(recipe);

		// Publish-before-save so the outbox row commits in the same Postgres
		// transaction as the row removal. Subscribers (Shopping clears
		// recipe references; Users records the deletion) won't observe a
		// "recipe is gone but no event arrived" half-state.
		await eventBus.PublishAsync(
			new RecipeDeletedIntegrationEvent(owner.Value, identifier.Value),
			cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success();
	}
}
