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
        int page,
        int pageSize,
        string sortBy,
        SortDirection sortDirection,
        string? search)
    {
        var sortField = Enum.TryParse<RecipeSortField>(sortBy, ignoreCase: true, out var parsed)
            ? parsed
            : RecipeSortField.Date;

        return new GetRecipesQuery(
            PagingOptions.Of(Page.From(page), PageSize.From(pageSize)),
            new SortingOptions<RecipeSortField>(sortField, sortDirection),
            SearchTerm.FromNullable(search));
    }
}
