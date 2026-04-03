using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class StepNumberConverter()
    : ValueConverter<StepNumber, int>(v => v.Value, v => StepNumber.From(v));
