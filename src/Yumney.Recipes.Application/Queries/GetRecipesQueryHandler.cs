using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

#pragma warning disable SA1601 // Partial elements should be documented (required for LoggerMessage source generation)
public sealed partial class GetRecipesQueryHandler(
    IRecipeRepository recipes,
    ICurrentUser currentUser,
    ILogger<GetRecipesQueryHandler> logger)
    : IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>
{
    public async Task<Result<PagedResult<RecipeListItemDto>>> HandleAsync(GetRecipesQuery query, CancellationToken cancellationToken = default)
    {
        var (page, pageSize, sortBy, sortDirection) = query;
        var owner = new OwnerIdentifier(currentUser.UserId);

        LogGetRecipes(owner.Value, page.Value, pageSize.Value);

        var skip = page.SkipCount(pageSize);

        var (items, totalCount) = await recipes.GetByOwnerAsync(
            owner, skip, pageSize.Value, sortBy, sortDirection, cancellationToken);

        var dtoItems = items.Select(r => new RecipeListItemDto(
            r.Id,
            r.Title.Value,
            r.Description?.Value,
            r.Servings?.Value,
            r.PreparationTime?.Value,
            r.CookingTime?.Value,
            r.Difficulty?.Value,
            r.ImageUrl?.Value,
            r.CreatedAt)).ToList();

        return Result<PagedResult<RecipeListItemDto>>.Success(
            new PagedResult<RecipeListItemDto>(dtoItems, totalCount, page.Value, pageSize.Value));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching recipes for owner {OwnerId}, page {Page}, pageSize {PageSize}")]
    private partial void LogGetRecipes(string ownerId, int page, int pageSize);
}
