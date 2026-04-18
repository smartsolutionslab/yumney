using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class PreparationTimeConverter()
	: ValueConverter<PreparationTime, int>(v => v.Value, v => PreparationTime.From(v));
