using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IDietaryProfileProvider
{
	Task<DietaryProfileSnapshot> GetAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);
}
