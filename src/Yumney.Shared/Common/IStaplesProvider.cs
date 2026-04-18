namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Cross-module interface for checking user staples.
/// Implemented by Users.Infrastructure, consumed by MealPlan.Application.
/// </summary>
public interface IStaplesProvider
{
	Task<IReadOnlySet<string>> GetStapleNamesAsync(
		string ownerId,
		CancellationToken cancellationToken = default);
}
