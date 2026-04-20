using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

public sealed class WeeklyPlanRepository(MealPlanDbContext context) : IWeeklyPlanRepository
{
#pragma warning disable SA1311
	private readonly DbSet<WeeklyPlan> plans = context.WeeklyPlans;
#pragma warning restore SA1311

	public async Task<WeeklyPlan?> FindByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		return await plans
			.AsNoTracking()
			.Include(p => p.Slots)
			.FirstOrDefaultAsync(p => p.Owner == owner && p.Week == week, cancellationToken);
	}

	public async Task<WeeklyPlan?> FindForUpdateAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		return await plans
			.Include(p => p.Slots)
			.FirstOrDefaultAsync(p => p.Owner == owner && p.Week == week, cancellationToken);
	}

	public async Task<WeeklyPlan> GetByOwnerAndWeekAsync(OwnerIdentifier owner, WeekIdentifier week, CancellationToken cancellationToken = default)
	{
		return await plans
			.Include(p => p.Slots)
			.FirstOrDefaultAsync(p => p.Owner == owner && p.Week == week, cancellationToken)
			?? throw new EntityNotFoundException(nameof(WeeklyPlan), $"{owner.Value}/{week.Value}");
	}

	public async Task AddAsync(WeeklyPlan plan, CancellationToken cancellationToken = default)
	{
		await plans.AddAsync(plan, cancellationToken);
	}
}
