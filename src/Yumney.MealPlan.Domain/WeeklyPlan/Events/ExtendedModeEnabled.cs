using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record ExtendedModeEnabled(SlotServings DefaultServings) : DomainEvent;
