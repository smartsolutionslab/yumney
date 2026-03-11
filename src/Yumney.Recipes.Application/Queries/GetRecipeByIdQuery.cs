using Yumney.Recipes.Application.DTOs;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Recipes.Application.Queries;

public sealed record GetRecipeByIdQuery(Guid Identifier) : IQuery<Result<RecipeDetailDto>>;
