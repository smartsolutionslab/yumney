using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class SaveRecipeCommandHandler(
	IRecipesUnitOfWork unitOfWork,
	ICurrentUser currentUser,
	IEventBus eventBus)
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

		var ingredients = ingredientCommands.Select(ingredient => ingredient.ToDomain()).ToList();
		var steps = stepCommands.Select(step => step.ToDomain()).ToList();

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

		await eventBus.PublishAsync(
			new RecipeImportedIntegrationEvent(owner.Value, recipe.Id.Value, recipe.Title.Value),
			cancellationToken);

		return recipe.ToSavedDto();
	}
}
