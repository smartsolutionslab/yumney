using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class UpdateRecipeNotesCommandHandler(IRecipesUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<UpdateRecipeNotesCommand, Result>
{
	public async Task<Result> HandleAsync(UpdateRecipeNotesCommand command, CancellationToken cancellationToken = default)
	{
		var (identifier, notes) = command;
		var recipe = await unitOfWork.Recipes.GetByIdForUpdateAsync(identifier, cancellationToken);

		if (recipe.Owner != currentUser.AsOwner()) return Result.Failure(UpdateRecipeNotesErrors.AccessDenied);

		recipe.UpdateNotes(notes);
		await unitOfWork.SaveChangesAsync(cancellationToken);
		return Result.Success();
	}
}
