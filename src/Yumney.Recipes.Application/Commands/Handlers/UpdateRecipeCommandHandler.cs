using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class UpdateRecipeCommandHandler(IRecipesUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>>
{
	public async Task<Result<RecipeDetailDto>> HandleAsync(UpdateRecipeCommand command, CancellationToken cancellationToken = default)
	{
		var (identifier, title, ingredientCommands, stepCommands, description, servings,
			 timing, difficulty, imageUrl, tags) = command;

		var owner = currentUser.AsOwner();

		var recipe = await unitOfWork.Recipes.GetByIdForUpdateAsync(identifier, cancellationToken);

		if (recipe.Owner != owner)
		{
			return UpdateRecipeErrors.AccessDenied;
		}

		var ingredients = ingredientCommands.Select(ingredient => ingredient.ToDomain()).ToList();
		var steps = stepCommands.Select(step => step.ToDomain()).ToList();

		recipe.Update(
			title,
			ingredients,
			steps,
			description,
			servings,
			timing,
			difficulty,
			imageUrl,
			tags);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return recipe.ToDetailDto();
	}
}
