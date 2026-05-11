namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IRecipeTagsLookup
{
	Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetAllAsync(CancellationToken cancellationToken = default);
}
