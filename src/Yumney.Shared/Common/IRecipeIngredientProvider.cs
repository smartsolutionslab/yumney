using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public interface IRecipeIngredientProvider
{
	Task<IReadOnlyList<RecipeIngredientInfo>> GetIngredientsAsync(Guid recipeIdentifier, CancellationToken cancellationToken = default);
}
