using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class GenerateShoppingListCommandHandler(
    IWeeklyPlanRepository plans,
    IRecipeIngredientProvider ingredientProvider,
    IStaplesProvider staplesProvider,
    IShoppingListWriter shoppingListWriter,
    ICurrentUser currentUser) : ICommandHandler<GenerateShoppingListCommand, Result<GenerateShoppingListResultDto>>
{
    public async Task<Result<GenerateShoppingListResultDto>> HandleAsync(GenerateShoppingListCommand command, CancellationToken cancellationToken = default)
    {
        var week = command.Week;
        var owner = currentUser.AsOwner();

        var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);
        if (plan is null) return GenerateShoppingListErrors.NoPlanFound;

        var recipeSlots = plan.Slots
            .Where(s => s.ContentType == SlotContentType.Recipe && s.Recipe is not null)
            .ToList();

        if (recipeSlots.Count == 0) return GenerateShoppingListErrors.NoRecipes;

        // Fetch ingredients for all recipes
        var merged = new Dictionary<string, MergedItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var slot in recipeSlots)
        {
            var ingredients = await ingredientProvider.GetIngredientsAsync(slot.Recipe!.RecipeIdentifier, cancellationToken);
            var recipeServings = (ingredients.Count > 0 ? ingredients[0].RecipeServings : null) ?? slot.Servings.Value;
            var scaleFactor = recipeServings > 0 ? (decimal)slot.Servings.Value / recipeServings : 1m;

            foreach (var ingredient in ingredients)
            {
                var scaledAmount = ingredient.Amount.HasValue
                    ? Math.Round(ingredient.Amount.Value * scaleFactor, 2)
                    : 0m;

                var key = $"{ingredient.Name.ToLowerInvariant()}|{ingredient.Unit ?? string.Empty}";
                if (merged.TryGetValue(key, out var existing))
                {
                    existing.Quantity += scaledAmount;
                }
                else
                {
                    merged[key] = new MergedItem(ingredient.Name, scaledAmount, ingredient.Unit);
                }
            }
        }

        // Filter out staples
        var staples = await staplesProvider.GetStapleNamesAsync(currentUser.UserId, cancellationToken);
        var staplesSkipped = 0;
        var itemsToAdd = new List<ShoppingItemRequest>();

        foreach (var item in merged.Values)
        {
            if (staples.Contains(item.Name.ToLowerInvariant()))
            {
                staplesSkipped++;
                continue;
            }

            itemsToAdd.Add(new ShoppingItemRequest(item.Name, item.Quantity, item.Unit, "meal-plan"));
        }

        if (itemsToAdd.Count > 0) await shoppingListWriter.AddItemsAsync(currentUser.UserId, itemsToAdd, cancellationToken);

        return new GenerateShoppingListResultDto(itemsToAdd.Count, staplesSkipped);
    }

    private sealed class MergedItem(string name, decimal quantity, string? unit)
    {
        public string Name { get; } = name;

        public decimal Quantity { get; set; } = quantity;

        public string? Unit { get; } = unit;
    }
}
