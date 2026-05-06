using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IDietaryProfileProvider
{
	Task<DietaryProfileSnapshot> GetAsync(CancellationToken cancellationToken = default);
}
