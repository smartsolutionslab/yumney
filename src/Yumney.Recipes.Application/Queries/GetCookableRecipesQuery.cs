using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public sealed record GetCookableRecipesQuery(bool FullMatchOnly = false)
	: IQuery<Result<IReadOnlyList<CookableRecipeDto>>>;
