using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Recipes.Application.Commands;

public sealed record ImportRecipeCommand(RecipeUrl Url) : ICommand<Result<ImportRecipeResultDto>>;

public sealed record ImportRecipeResultDto(string Message);
