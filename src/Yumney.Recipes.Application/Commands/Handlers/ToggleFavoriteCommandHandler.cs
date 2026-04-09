using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (LoggerMessage generates partial methods)
public sealed partial class ToggleFavoriteCommandHandler(
    IRecipeRepository recipes,
    IRecipeFavoriteRepository favorites,
    ICurrentUser currentUser,
    ILogger<ToggleFavoriteCommandHandler> logger)
    : ICommandHandler<ToggleFavoriteCommand, Result<FavoriteStateDto>>
{
    public async Task<Result<FavoriteStateDto>> HandleAsync(ToggleFavoriteCommand command, CancellationToken cancellationToken = default)
    {
        var identifier = command.Identifier;
        var owner = currentUser.AsOwner();

        var recipe = await recipes.GetByIdAsync(identifier, cancellationToken);
        if (recipe is null)
        {
            LogRecipeNotFound(identifier);
            return ToggleFavoriteErrors.NotFound;
        }

        if (recipe.Owner != owner)
        {
            LogRecipeAccessDenied(identifier, owner.Value);
            return ToggleFavoriteErrors.AccessDenied;
        }

        var alreadyFavorited = await favorites.IsFavoritedAsync(owner, identifier, cancellationToken);

        if (alreadyFavorited)
        {
            await favorites.RemoveAsync(owner, identifier, cancellationToken);
            LogRecipeUnfavorited(identifier, owner.Value);
            return new FavoriteStateDto(identifier.Value, false);
        }

        var favorite = RecipeFavorite.Create(identifier, owner);
        await favorites.AddAsync(favorite, cancellationToken);
        LogRecipeFavorited(identifier, owner.Value);
        return new FavoriteStateDto(identifier.Value, true);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recipe {RecipeIdentifier} not found for favorite toggle")]
    private partial void LogRecipeNotFound(RecipeIdentifier recipeIdentifier);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Access denied to favorite recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogRecipeAccessDenied(RecipeIdentifier recipeIdentifier, string ownerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe {RecipeIdentifier} favorited by owner {OwnerId}")]
    private partial void LogRecipeFavorited(RecipeIdentifier recipeIdentifier, string ownerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe {RecipeIdentifier} unfavorited by owner {OwnerId}")]
    private partial void LogRecipeUnfavorited(RecipeIdentifier recipeIdentifier, string ownerId);
}
