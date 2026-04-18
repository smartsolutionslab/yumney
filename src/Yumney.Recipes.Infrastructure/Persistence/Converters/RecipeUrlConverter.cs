using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeUrlConverter()
	: ValueConverter<RecipeUrl, string>(v => v.Value, v => RecipeUrl.From(v));
