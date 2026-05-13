using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record RateRecipeCommand(RecipeIdentifier Identifier, Rating Rating) : ICommand<Result>;
