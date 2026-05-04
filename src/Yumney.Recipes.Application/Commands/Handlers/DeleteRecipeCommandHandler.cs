using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class DeleteRecipeCommandHandler(
	IRecipesUnitOfWork unitOfWork,
	ICurrentUser currentUser,
	IEventBus eventBus)
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
		await unitOfWork.SaveChangesAsync(cancellationToken);

		await eventBus.PublishAsync(
			new RecipeDeletedIntegrationEvent(owner.Value, identifier.Value),
			cancellationToken);

		return Result.Success();
	}
}
