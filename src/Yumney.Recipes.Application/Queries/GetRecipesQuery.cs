using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public sealed record GetRecipesQuery(
    PagingOptions Paging,
    SortingOptions<RecipeSortField> Sorting,
    SearchTerm? Search = null) : IQuery<Result<PagedResult<RecipeListItemDto>>>
{
    public static GetRecipesQuery From(
        int page, int pageSize, string sortBy, SortDirection sortDirection, string? search = null)
    {
        var clampedPage = new Page(Math.Max(page, 1));
        var clampedSize = new PageSize(Math.Clamp(pageSize, 1, PagingOptions.MaxPageSize));
        var paging = PagingOptions.From(clampedPage, clampedSize);

        var parsedSortBy = Enum.TryParse<RecipeSortField>(sortBy, ignoreCase: true, out var field)
            ? field
            : RecipeSortField.Date;
        var sorting = new SortingOptions<RecipeSortField>(parsedSortBy, sortDirection);

        var searchTerm = SearchTerm.FromNullable(search);
        return new GetRecipesQuery(paging, sorting, searchTerm);
    }
}
