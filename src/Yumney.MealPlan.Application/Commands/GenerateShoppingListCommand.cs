using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public sealed record GenerateShoppingListCommand(int Year, int WeekNumber) : ICommand<Result<GenerateShoppingListResult>>;

public sealed record GenerateShoppingListResult(int ItemsAdded, int StaplesSkipped);
