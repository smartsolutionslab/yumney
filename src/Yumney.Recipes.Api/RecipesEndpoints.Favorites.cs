using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

#pragma warning disable SA1601 // Partial endpoints class is split for file-size reasons.
public static partial class RecipesEndpoints
#pragma warning restore SA1601
{
    private static async Task<IResult> ToggleFavoriteAsync(
        Guid identifier,
        ICommandHandler<ToggleFavoriteCommand, Result<FavoriteStateDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ToggleFavoriteCommand(RecipeIdentifier.From(identifier));
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }
}
