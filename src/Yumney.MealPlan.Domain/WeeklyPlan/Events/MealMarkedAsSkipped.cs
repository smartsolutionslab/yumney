using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record MealMarkedAsSkipped(DayOfWeek Day, MealType MealType) : DomainEvent;
