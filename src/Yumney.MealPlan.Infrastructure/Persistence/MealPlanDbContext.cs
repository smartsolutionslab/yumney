using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

public sealed class MealPlanDbContext(DbContextOptions<MealPlanDbContext> options) : DbContext(options)
{
    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MealPlanDbContext).Assembly);
    }
}
