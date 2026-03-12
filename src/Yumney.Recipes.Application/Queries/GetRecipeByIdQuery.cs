using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public sealed record GetRecipeByIdQuery(RecipeIdentifier Identifier) : IQuery<Result<RecipeDetailDto>>
{
    public static GetRecipeByIdQuery From(Guid identifier) => new(new RecipeIdentifier(identifier));
}
