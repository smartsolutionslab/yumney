using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

/// <summary>
/// "I froze it" override (US-341): user reclassifies an at-home item as
/// frozen, resetting the freshness clock so the projection nudge reflects
/// the longer shelf life.
/// </summary>
public sealed record MarkAsFrozenCommand(ItemName ItemName, Unit? Unit) : ICommand<Result>;
