using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeUnitConverter()
    : ValueConverter<Unit, string>(v => v.Value, v => Unit.From(v));
