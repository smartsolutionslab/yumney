using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeLanguageConverter()
	: ValueConverter<RecipeLanguage, string>(v => v.Value, v => RecipeLanguage.From(v));
