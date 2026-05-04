using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

/// <summary>
/// Records that the user finished cooking the recipe (US-121). Publishes a
/// <c>RecipeCookedIntegrationEvent</c> for the Users module to log.
/// </summary>
public sealed record TrackRecipeCookedCommand(RecipeIdentifier Identifier) : ICommand<Result>;
