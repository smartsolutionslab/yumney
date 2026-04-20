using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

public sealed class MealPlanUnitOfWork(MealPlanDbContext context, IWeeklyPlanRepository plans) : IMealPlanUnitOfWork
{
	public IWeeklyPlanRepository Plans => plans;

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> context.SaveChangesAsync(cancellationToken);
}
