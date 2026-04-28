using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record MealResetToPlanned(DayOfWeek Day, MealType MealType) : DomainEvent;
