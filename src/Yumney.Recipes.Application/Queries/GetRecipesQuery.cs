using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public sealed record GetRecipesQuery(
	PagingOptions Paging,
	SortingOptions<RecipeSortField> Sorting,
	SearchTerm? Search = null,
	RecipeFilter? Filter = null) : IQuery<Result<PagedResult<RecipeListItemDto>>>;
