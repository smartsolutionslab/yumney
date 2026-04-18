using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class IngredientNameConverter()
	: ValueConverter<IngredientName, string>(v => v.Value, v => IngredientName.From(v));
