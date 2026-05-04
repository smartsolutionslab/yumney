using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record MealMarkedAsSkipped(DayOfWeek Day, MealType MealType) : DomainEvent;
