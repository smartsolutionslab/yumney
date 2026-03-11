using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public sealed record GetRecipesQuery(
    Page Page,
    PageSize PageSize,
    RecipeSortField SortBy,
    SortDirection SortDirection) : IQuery<Result<PagedResult<RecipeListItemDto>>>
{
    public static GetRecipesQuery FromRequest(
        int page, int pageSize, RecipeSortField sortBy, SortDirection sortDirection)
    {
        var clampedPage = Math.Max(page, 1);
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);
        return new GetRecipesQuery(new Page(clampedPage), new PageSize(clampedPageSize), sortBy, sortDirection);
    }
}
