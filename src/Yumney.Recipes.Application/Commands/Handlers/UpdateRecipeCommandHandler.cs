using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class UpdateRecipeCommandHandler(
	IRecipeRepository recipes,
	ICurrentUser currentUser)
	: ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>>
{
	public async Task<Result<RecipeDetailDto>> HandleAsync(UpdateRecipeCommand command, CancellationToken cancellationToken = default)
	{
		var (identifier, title, ingredientCommands, stepCommands, description, servings,
			 timing, difficulty, imageUrl, tags) = command;

		var owner = currentUser.AsOwner();

		var recipe = await recipes.GetByIdForUpdateAsync(identifier, cancellationToken);

		if (recipe.Owner != owner)
		{
			return UpdateRecipeErrors.AccessDenied;
		}

		var ingredients = ingredientCommands.Select(i => i.ToDomain()).ToList();
		var steps = stepCommands.Select(s => s.ToDomain()).ToList();

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

		await recipes.UpdateAsync(recipe, cancellationToken);

		return recipe.ToDetailDto();
	}
}
