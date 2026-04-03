using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeTitleConverter()
    : ValueConverter<RecipeTitle, string>(v => v.Value, v => RecipeTitle.From(v));
