using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (required for LoggerMessage source generation)
public sealed partial class GetRecipeByIdQueryHandler(
    IRecipeRepository recipes,
    IRecipeFavoriteRepository favorites,
    ICurrentUser currentUser,
    ILogger<GetRecipeByIdQueryHandler> logger)
    : IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>>
{
    public async Task<Result<RecipeDetailDto>> HandleAsync(GetRecipeByIdQuery query, CancellationToken cancellationToken = default)
    {
        var identifier = query.Identifier;
        var owner = currentUser.AsOwner();

        LogGetRecipeById(identifier, owner.Value);

        var recipe = await recipes.GetByIdAsync(identifier, cancellationToken);

        if (recipe.Owner != owner)
        {
            LogRecipeAccessDenied(identifier, owner.Value);
            return GetRecipeByIdErrors.AccessDenied;
        }

        var isFavorite = await favorites.IsFavoritedAsync(owner, identifier, cancellationToken);
        return recipe.ToDetailDto(isFavorite);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogGetRecipeById(RecipeIdentifier recipeIdentifier, string ownerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Access denied to recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogRecipeAccessDenied(RecipeIdentifier recipeIdentifier, string ownerId);
}
