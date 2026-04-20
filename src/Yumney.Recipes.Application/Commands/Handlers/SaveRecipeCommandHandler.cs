using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class SaveRecipeCommandHandler(
	IRecipesUnitOfWork unitOfWork,
	ICurrentUser currentUser)
	: ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>>
{
	public async Task<Result<SavedRecipeDto>> HandleAsync(SaveRecipeCommand command, CancellationToken cancellationToken = default)
	{
		var (title, ingredientCommands, stepCommands, description, servings, timing, difficulty,
			imageUrl, language, sourceUrl, tags) = command;

		var owner = currentUser.AsOwner();

		if (sourceUrl is not null && await unitOfWork.Recipes.ExistsBySourceUrlAsync(sourceUrl, owner, cancellationToken))
		{
			return SaveRecipeErrors.AlreadyImported;
		}

		var ingredients = ingredientCommands.Select(i => i.ToDomain()).ToList();
		var steps = stepCommands.Select(s => s.ToDomain()).ToList();

		var recipe = Recipe.Create(
			title,
			owner,
			ingredients,
			steps,
			description,
			servings,
			timing,
			difficulty,
			imageUrl,
			language,
			sourceUrl,
			tags);

		await unitOfWork.Recipes.AddAsync(recipe, cancellationToken);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return recipe.ToSavedDto();
	}
}
