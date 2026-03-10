using Microsoft.Extensions.Logging;
using Yumney.Recipes.Application.DTOs;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Recipes.Application.Commands;

#pragma warning disable SA1601 // Partial elements should be documented (LoggerMessage generates partial methods)
public sealed partial class SaveRecipeCommandHandler(
    IRecipeRepository recipes,
    ICurrentUser currentUser,
    ILogger<SaveRecipeCommandHandler> logger)
    : ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>>
{
    public async Task<Result<SavedRecipeDto>> HandleAsync(SaveRecipeCommand command, CancellationToken cancellationToken = default)
    {
        var (title, ingredientCommands, stepCommands, description, servings, preparationTime, cookingTime, difficulty, imageUrl, sourceUrl) = command;

        var owner = new OwnerIdentifier(currentUser.UserId);

        if (sourceUrl is not null && await recipes.ExistsBySourceUrlAsync(sourceUrl, owner, cancellationToken))
        {
            LogDuplicateImport(sourceUrl.Value, owner.Value);
            return Result<SavedRecipeDto>.Failure(SaveRecipeErrors.AlreadyImported);
        }

        var ingredients = ingredientCommands
            .Select(i => Ingredient.Create(i.Name, i.Amount, i.Unit))
            .ToList();

        var steps = stepCommands
            .Select(s => Step.Create(s.Number, s.Description))
            .ToList();

        var recipe = Recipe.Create(
            title,
            owner,
            ingredients,
            steps,
            description,
            servings,
            preparationTime,
            cookingTime,
            difficulty,
            imageUrl,
            sourceUrl);

        await recipes.AddAsync(recipe, cancellationToken);

        LogRecipeSaved(recipe.Id, title.Value);

        return Result<SavedRecipeDto>.Success(
            new SavedRecipeDto(recipe.Id, title.Value, recipe.CreatedAt));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Duplicate import attempt for URL {SourceUrl} by owner {Owner}")]
    private partial void LogDuplicateImport(string sourceUrl, string owner);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe {RecipeId} '{Title}' saved successfully")]
    private partial void LogRecipeSaved(Guid recipeId, string title);
}
