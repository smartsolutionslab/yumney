using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record UpdateRecipeNotesCommand(RecipeIdentifier Identifier, Notes? Notes) : ICommand<Result>;
