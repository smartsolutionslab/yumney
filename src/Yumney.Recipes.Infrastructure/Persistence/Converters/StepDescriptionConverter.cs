using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class StepDescriptionConverter()
    : ValueConverter<StepDescription, string>(v => v.Value, v => StepDescription.From(v));
