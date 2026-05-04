using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public sealed record GetCookableRecipesQuery(bool FullMatchOnly = false)
	: IQuery<Result<IReadOnlyList<CookableRecipeDto>>>;
