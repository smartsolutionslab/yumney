using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (LoggerMessage generates partial methods)
public sealed partial class DeleteRecipeCommandHandler(
    IRecipeRepository recipes,
    ICurrentUser currentUser,
    ILogger<DeleteRecipeCommandHandler> logger)
    : ICommandHandler<DeleteRecipeCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteRecipeCommand command, CancellationToken cancellationToken = default)
    {
        var identifier = command.Identifier;
        var owner = new OwnerIdentifier(currentUser.UserId);

        LogDeleteRecipe(identifier, owner.Value);

        var recipe = await recipes.GetByIdAsync(identifier, cancellationToken);

        if (recipe is null)
        {
            LogRecipeNotFound(identifier);
            return Result.Failure(DeleteRecipeErrors.NotFound);
        }

        if (recipe.Owner != owner)
        {
            LogRecipeAccessDenied(identifier, owner.Value);
            return Result.Failure(DeleteRecipeErrors.AccessDenied);
        }

        recipe.MarkAsDeleted();

        await recipes.DeleteAsync(recipe, cancellationToken);

        LogRecipeDeleted(recipe.Id, recipe.Title.Value);

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleting recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogDeleteRecipe(RecipeIdentifier recipeIdentifier, string ownerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recipe {RecipeIdentifier} not found for deletion")]
    private partial void LogRecipeNotFound(RecipeIdentifier recipeIdentifier);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Access denied to delete recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogRecipeAccessDenied(RecipeIdentifier recipeIdentifier, string ownerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe {RecipeIdentifier} '{Title}' deleted successfully")]
    private partial void LogRecipeDeleted(Guid recipeIdentifier, string title);
}
