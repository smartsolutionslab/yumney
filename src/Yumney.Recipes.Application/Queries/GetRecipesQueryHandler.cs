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
        var (paging, sorting) = query;
        var owner = new OwnerIdentifier(currentUser.UserId);

        LogGetRecipes(owner.Value, paging.Page.Value, paging.PageSize.Value);

        var (items, totalCount) = await recipes.GetByOwnerAsync(
            owner, paging, sorting, cancellationToken);

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
            new PagedResult<RecipeListItemDto>(dtoItems, totalCount, paging.Page.Value, paging.PageSize.Value));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching recipes for owner {OwnerId}, page {Page}, pageSize {PageSize}")]
    private partial void LogGetRecipes(string ownerId, int page, int pageSize);
}
