using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public interface IMealPlanUnitOfWork : IUnitOfWork
{
	IWeeklyPlanRepository Plans { get; }
}
