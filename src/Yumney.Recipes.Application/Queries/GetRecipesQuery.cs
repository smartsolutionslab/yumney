using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public sealed record GetRecipesQuery(
    int Page,
    int PageSize,
    RecipeSortField SortBy,
    SortDirection SortDirection) : IQuery<Result<RecipeListDto>>;
