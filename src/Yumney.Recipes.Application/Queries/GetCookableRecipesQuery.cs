using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public sealed record GetCookableRecipesQuery(PagingOptions Paging, bool FullMatchOnly = false)
	: IQuery<Result<PagedResult<CookableRecipeDto>>>;
