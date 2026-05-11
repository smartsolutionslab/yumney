namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IDietaryProfileProvider
{
	Task<DietaryProfileSnapshot> GetAsync(CancellationToken cancellationToken = default);
}

public sealed record DietaryProfileSnapshot(string? DietaryType, IReadOnlyList<string> Restrictions)
{
	public static DietaryProfileSnapshot Empty { get; } = new(null, []);
}
