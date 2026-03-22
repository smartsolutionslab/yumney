using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (LoggerMessage generates partial methods)
public sealed partial class UpdateRecipeCommandHandler(
    IRecipeRepository recipes,
    ICurrentUser currentUser,
    ILogger<UpdateRecipeCommandHandler> logger)
    : ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>>
{
    public async Task<Result<RecipeDetailDto>> HandleAsync(UpdateRecipeCommand command, CancellationToken cancellationToken = default)
    {
        var (identifier, title, ingredientCommands, stepCommands, description, servings,
             preparationTime, cookingTime, difficulty, imageUrl, tags) = command;

        var owner = new OwnerIdentifier(currentUser.UserId);

        LogUpdateRecipe(identifier, owner.Value);

        var recipe = await recipes.GetByIdAsync(identifier, cancellationToken);

        if (recipe is null)
        {
            LogRecipeNotFound(identifier);
            return Result<RecipeDetailDto>.Failure(UpdateRecipeErrors.NotFound);
        }

        if (recipe.Owner != owner)
        {
            LogRecipeAccessDenied(identifier, owner.Value);
            return Result<RecipeDetailDto>.Failure(UpdateRecipeErrors.AccessDenied);
        }

        var ingredients = ingredientCommands
            .Select(i => Ingredient.Create(i.Name, i.Amount, i.Unit))
            .ToList();

        var steps = stepCommands
            .Select(s => Step.Create(s.Number, s.Description))
            .ToList();

        recipe.Update(
            title,
            ingredients,
            steps,
            description,
            servings,
            preparationTime,
            cookingTime,
            difficulty,
            imageUrl,
            tags);

        await recipes.UpdateAsync(recipe, cancellationToken);

        LogRecipeUpdated(recipe.Id.Value, title.Value);

        return Result<RecipeDetailDto>.Success(recipe.ToDetailDto());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Updating recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogUpdateRecipe(RecipeIdentifier recipeIdentifier, string ownerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recipe {RecipeIdentifier} not found for update")]
    private partial void LogRecipeNotFound(RecipeIdentifier recipeIdentifier);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Access denied to update recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogRecipeAccessDenied(RecipeIdentifier recipeIdentifier, string ownerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe {RecipeIdentifier} '{Title}' updated successfully")]
    private partial void LogRecipeUpdated(Guid recipeIdentifier, string title);
}
