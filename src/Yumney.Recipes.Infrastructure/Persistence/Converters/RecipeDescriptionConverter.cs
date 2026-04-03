using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeDescriptionConverter()
    : ValueConverter<RecipeDescription, string>(v => v.Value, v => RecipeDescription.From(v));
