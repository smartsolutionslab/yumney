using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeAmountConverter()
    : ValueConverter<Amount, decimal>(v => v.Value, v => Amount.From(v));
