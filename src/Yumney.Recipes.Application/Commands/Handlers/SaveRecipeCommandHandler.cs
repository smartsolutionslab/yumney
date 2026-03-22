using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (LoggerMessage generates partial methods)
public sealed partial class SaveRecipeCommandHandler(
    IRecipeRepository recipes,
    ICurrentUser currentUser,
    ILogger<SaveRecipeCommandHandler> logger)
    : ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>>
{
    public async Task<Result<SavedRecipeDto>> HandleAsync(SaveRecipeCommand command, CancellationToken cancellationToken = default)
    {
        var (title, ingredientCommands, stepCommands, description, servings, preparationTime, cookingTime, difficulty, imageUrl, language, sourceUrl, tags) = command;

        var owner = new OwnerIdentifier(currentUser.UserId);

        if (sourceUrl is not null && await recipes.ExistsBySourceUrlAsync(sourceUrl, owner, cancellationToken))
        {
            LogDuplicateImport(sourceUrl.Value, owner.Value);
            return Result<SavedRecipeDto>.Failure(SaveRecipeErrors.AlreadyImported);
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
            preparationTime,
            cookingTime,
            difficulty,
            imageUrl,
            language,
            sourceUrl,
            tags);

        await recipes.AddAsync(recipe, cancellationToken);

        LogRecipeSaved(recipe.Id.Value, title.Value);
        return Result<SavedRecipeDto>.Success(recipe.ToSavedDto());
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Duplicate import attempt for URL {SourceUrl} by owner {Owner}")]
    private partial void LogDuplicateImport(string sourceUrl, string owner);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe {RecipeIdentifier} '{Title}' saved successfully")]
    private partial void LogRecipeSaved(Guid recipeIdentifier, string title);
}
