using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class DifficultyConverter()
    : ValueConverter<Difficulty, string>(v => v.Value, v => Difficulty.From(v));
