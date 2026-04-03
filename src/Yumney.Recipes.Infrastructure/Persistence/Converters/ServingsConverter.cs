using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class ServingsConverter()
    : ValueConverter<Servings, int>(v => v.Value, v => Servings.From(v));
