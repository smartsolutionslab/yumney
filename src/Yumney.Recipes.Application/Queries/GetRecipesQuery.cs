using Yumney.Recipes.Application.DTOs;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Recipes.Application.Queries;

public sealed record GetRecipesQuery(
    int Page,
    int PageSize,
    RecipeSortField SortBy,
    SortDirection SortDirection) : IQuery<Result<RecipeListDto>>;
