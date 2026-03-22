using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (required for LoggerMessage source generation)
public sealed partial class GetRecipesQueryHandler(
    IRecipeRepository recipes,
    ICurrentUser currentUser,
    ILogger<GetRecipesQueryHandler> logger)
    : IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>
{
    public async Task<Result<PagedResult<RecipeListItemDto>>> HandleAsync(GetRecipesQuery query, CancellationToken cancellationToken = default)
    {
        var (paging, sorting, search) = query;
        var owner = new OwnerIdentifier(currentUser.UserId);

        LogGetRecipes(owner.Value, paging.Page.Value, paging.PageSize.Value, search?.Value);

        var (items, totalCount) = await recipes.GetByOwnerAsync(
            owner, paging, sorting, search, cancellationToken);

        var dtoItems = items.Select(r => r.ToListItemDto()).ToList();

        return Result<PagedResult<RecipeListItemDto>>.Success(
            new PagedResult<RecipeListItemDto>(dtoItems, totalCount, paging.Page.Value, paging.PageSize.Value));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching recipes for owner {OwnerId}, page {Page}, pageSize {PageSize}, search {Search}")]
    private partial void LogGetRecipes(string ownerId, int page, int pageSize, string? search);
}
