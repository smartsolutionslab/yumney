using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Common;

public sealed record MergedIngredient(ItemName Name, Quantity? Quantity);
