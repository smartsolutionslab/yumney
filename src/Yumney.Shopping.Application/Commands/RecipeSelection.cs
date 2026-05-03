using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record RecipeSelection(RecipeReference Recipe, Servings? DesiredServings);
