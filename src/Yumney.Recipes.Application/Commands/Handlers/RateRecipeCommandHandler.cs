using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class RateRecipeCommandHandler(IRecipesUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<RateRecipeCommand, Result>
{
	public async Task<Result> HandleAsync(RateRecipeCommand command, CancellationToken cancellationToken = default)
	{
		var (identifier, rating) = command;
		var recipe = await unitOfWork.Recipes.GetByIdForUpdateAsync(identifier, cancellationToken);

		if (recipe.Owner != currentUser.AsOwner()) return Result.Failure(RateRecipeErrors.AccessDenied);

		recipe.RateAs(rating);
		await unitOfWork.SaveChangesAsync(cancellationToken);
		return Result.Success();
	}
}
