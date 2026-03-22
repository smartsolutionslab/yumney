using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (required for LoggerMessage source generation)
public sealed partial class GetRecipeByIdQueryHandler(IRecipeRepository recipes, ICurrentUser currentUser, ILogger<GetRecipeByIdQueryHandler> logger)
    : IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>>
{
    public async Task<Result<RecipeDetailDto>> HandleAsync(GetRecipeByIdQuery query, CancellationToken cancellationToken = default)
    {
        var identifier = query.Identifier;
        var owner = new OwnerIdentifier(currentUser.UserId);

        LogGetRecipeById(identifier, owner.Value);

        var recipe = await recipes.GetByIdAsync(identifier, cancellationToken);

        if (recipe is null)
        {
            LogRecipeNotFound(identifier);
            return Result<RecipeDetailDto>.Failure(GetRecipeByIdErrors.NotFound);
        }

        if (recipe.Owner != owner)
        {
            LogRecipeAccessDenied(identifier, owner.Value);
            return Result<RecipeDetailDto>.Failure(GetRecipeByIdErrors.AccessDenied);
        }

        return Result<RecipeDetailDto>.Success(recipe.ToDetailDto());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogGetRecipeById(RecipeIdentifier recipeIdentifier, string ownerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recipe {RecipeIdentifier} not found")]
    private partial void LogRecipeNotFound(RecipeIdentifier recipeIdentifier);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Access denied to recipe {RecipeIdentifier} for owner {OwnerId}")]
    private partial void LogRecipeAccessDenied(RecipeIdentifier recipeIdentifier, string ownerId);
}
