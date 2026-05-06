namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IStaplesProvider
{
	Task<IReadOnlySet<string>> GetStapleNamesAsync(CancellationToken cancellationToken = default);
}
