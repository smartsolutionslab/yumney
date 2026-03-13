using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record DeleteRecipeCommand(RecipeIdentifier Identifier) : ICommand<Result>
{
    public static DeleteRecipeCommand From(Guid identifier)
    {
        return new DeleteRecipeCommand(new RecipeIdentifier(identifier));
    }
}
