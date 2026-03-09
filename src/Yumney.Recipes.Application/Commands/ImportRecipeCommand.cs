using Yumney.Recipes.Application.DTOs;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Recipes.Application.Commands;

public sealed record ImportRecipeCommand(RecipeUrl Url)
    : ICommand<Result<ExtractedRecipeDto>>;
