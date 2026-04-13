using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class DeleteRecipeCommandHandler(
    IRecipeRepository recipes,
    ICurrentUser currentUser)
    : ICommandHandler<DeleteRecipeCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteRecipeCommand command, CancellationToken cancellationToken = default)
    {
        var identifier = command.Identifier;
        var owner = currentUser.AsOwner();

        var recipe = await recipes.GetByIdForUpdateAsync(identifier, cancellationToken);

        if (recipe.Owner != owner)
        {
            return Result.Failure(DeleteRecipeErrors.AccessDenied);
        }

        recipe.MarkAsDeleted();

        await recipes.DeleteAsync(recipe, cancellationToken);

        return Result.Success();
    }
}
