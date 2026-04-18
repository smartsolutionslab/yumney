using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class CookingTimeConverter()
	: ValueConverter<CookingTime, int>(v => v.Value, v => CookingTime.From(v));
